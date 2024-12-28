using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BezKolejki_bot.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using Services.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using SeleniumUndetectedChromeDriver;
using OpenQA.Selenium.Interactions;


namespace BezKolejki_bot.Services
{
    public class BrowserSiteProcessor : ISiteProcessor
    {
        private readonly ILogger<BrowserSiteProcessor> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IConfiguration _configuration;
        private readonly int _interval;
        private readonly ConcurrentDictionary<string, bool> _processingSite = new();
        private readonly Boolean _browserIsVisible = false;

        public BrowserSiteProcessor(ILogger<BrowserSiteProcessor> logger, IBezKolejkiService bezKolejkiService, IConfiguration configuration)
        {
            _logger = logger;
            _bezKolejkiService = bezKolejkiService;
            _configuration = configuration;
            var timeOutBrowser = configuration["ScheduledTask:TimeOutBrowser"];
            var browserIsVisible = configuration["ScheduledTask:BrowserIsVisible"];

            if (!Boolean.TryParse(browserIsVisible, out _browserIsVisible)) 
            {
                throw new InvalidOperationException("Browser visibility property not set");
            }

            if (!int.TryParse(timeOutBrowser, out _interval) || _interval <= 0)
            {
                throw new InvalidOperationException("Timeout Task Scheduled interval is not properly configured.");
            }
        }

        public async Task ProcessSiteAsync(string url, string code) 
        {
            if (_processingSite.ContainsKey(url))
            {
                _logger.LogInformation($"Site '{url}' is already being processed. Skipping...");
                return;
            }
            else {
                _processingSite.TryAdd(url, true);
            }

            try
            {

                var options = new ChromeOptions();
                //options.AddArgument("--disable-blink-features=AutomationControlled");
                //options.AddExcludedArgument("enable-automation");
                options.AddArgument("--no-sandbox");
                //options.AddArgument("start-maximized");
                //options.AddArgument("disable-infobars");
                //options.AddArgument("--disable-dev-shm-usage");
                //options.AddArgument("--enable-unsafe-swiftshader");
                //options.AddArgument("--window-size=1920x1080");

                //options.AddAdditionalOption("useAutomationExtension", false);
                //options.AddArgument("accept-language='ru-RU,ru;q=0.9'");
                //options.AddArgument("referer='https://bezkolejki.eu/luwbb/'");
                //options.AddArgument("sec-ch-ua='Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"'");
                //options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
                //options.AddArgument("--window-size=1,1");
                //options.AddArgument("user-agent='Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36'");
                options.AddArgument("--incognito");

                if (!_browserIsVisible) {
                    options.AddArgument("--headless");
                }


                var cancellationTokenSource = new CancellationTokenSource();
                var timer = new Timer(state =>
                {
                    _logger.LogWarning("Browser automation timed out. Closing the browser.");
                    cancellationTokenSource.Cancel();
                }, null, TimeSpan.FromSeconds(_interval), Timeout.InfiniteTimeSpan);

                try
                {
                    using (var driver = UndetectedChromeDriver.Create(options,
                            driverExecutablePath:
                                await new ChromeDriverInstaller().Auto()))
                    {
                        driver.Navigate().GoToUrl(url);
                        await Task.Delay(1500);

                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        await ExecuteBrowserAutomation(driver, wait, url, cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Browser automation was canceled due to timeout.");
                }
                finally
                {
                    timer.Dispose();
                }
            }
            finally
            {
                _processingSite.TryRemove(url, out _);
            }
        }

        private async Task ExecuteBrowserAutomation(IWebDriver driver, WebDriverWait wait, string url, CancellationToken cancellationToken)
        {
            try
            {
                var dates = new List<DateTime>();
                wait.Until(d => d.FindElement(By.Id("Operacja2")).FindElements(By.TagName("button")).Count > 0);

                var listOperacja2 = wait.Until(d => d.FindElement(By.Id("Operacja2")));
                var buttonTexts = listOperacja2.FindElements(By.TagName("button")).Select(b => b.Text).ToList();
                int index = 0;
                var options = driver.Manage().Network;
                await options.StartMonitoring();
                var activeUsers = await _bezKolejkiService.GetActiveUsers();

                while (index < buttonTexts.Count)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var buttonText = buttonTexts[index];
                    var buttonCode = CodeMapping.GetValueByKey(buttonText);

                    var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(buttonCode);

                    if (countByActiveUsers <= 0)
                    {
                        _logger.LogInformation($"{buttonCode} count subscribers = 0. skipping....");
                        index++;
                        continue;
                    }

                    _logger.LogInformation($"{buttonCode} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");

                    bool success = false;
                    bool dataSaved = false; 

                    EventHandler<NetworkResponseReceivedEventArgs> networkHandler = async (_, e) =>
                    {
                        if (e.ResponseUrl.Contains("api/Slot/GetAvailableDaysForOperation?com"))
                        {
                            var responseBody = e.ResponseBody;
                            if (responseBody != null && !string.Equals(responseBody, "\"Error while verify captcha\""))
                            {
                                try
                                {
                                    var settings = new JsonSerializerSettings { Error = (sender, args) => { args.ErrorContext.Handled = true; } };
                                    var data = JsonConvert.DeserializeObject<BezKolejkiJsonModel>(responseBody, settings);

                                    if (data != null)
                                    {
                                        var code = CodeMapping.GetCodeByOperationId(data.operationId);
                                        if (string.IsNullOrEmpty(code))
                                        {
                                            _logger.LogWarning($"Error mapping code {data.operationId}");
                                        }

                                        dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, data.availableDays, code);
                                    }
                                }
                                catch (JsonSerializationException ex)
                                {
                                    _logger.LogWarning($"Error deserializing JSON: {ex.Message}, Response: {responseBody}");
                                }
                                catch (Exception)
                                {
                                    _logger.LogInformation($"{buttonText} not a json object: {responseBody}");
                                }
                            }
                        }
                    };

                    try
                    {
                        options.NetworkResponseReceived += networkHandler; //Subscribe event
                        while (!success)
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var currentButtons = driver.FindElement(By.Id("Operacja2"))
                                                           .FindElements(By.TagName("button"))
                                                           .ToList();
                                var button = currentButtons.FirstOrDefault(b => b.Text == buttonText);
                                if (button == null)
                                {
                                    _logger.LogWarning($"Button with text '{buttonText}' not found. Skipping.");
                                    success = true;
                                    continue;
                                }

                                await Task.Delay(1500);
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                                await Task.Delay(2500);

                                await clickWizardIcon(driver);
                                await Task.Delay(2100);
                                cancellationToken.ThrowIfCancellationRequested();
                                await ClickButton(driver,  button);


                                await Task.Delay(1000);
                                wait.Until(d =>
                                {
                                    var loadingElement = driver.FindElement(By.CssSelector(".vld-overlay.is-active"));
                                    return loadingElement.GetCssValue("display") == "none";
                                });

                                await ErrorCaptchaAsync(driver, buttonText, cancellationToken);
                                index++;
                                success = true;
                            }
                            catch (StaleElementReferenceException ex)
                            {
                                _logger.LogInformation($"{buttonText}.Stale element detected. Retrying for current button. {ex.Message}");
                                await Task.Delay(1000);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"{buttonText}. Error event: {ex.Message}\nStackTrace: {ex.StackTrace}");
                            }
                            finally
                            {
                                success = true;
                                index++;
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogWarning($"Операция была отменена.при обработке кнопки {buttonText} {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{buttonText}. Error event: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                    finally
                    {
                        options.NetworkResponseReceived -= networkHandler;
                    }
                }
                await options.StopMonitoring();
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogInformation("Error: element not found or timeout");
            }
            catch (WebDriverException ex) when (ex.Message.Contains("disconnected"))
            {
                _logger.LogWarning($"Browser disconnected unexpectedly: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Browser disconnected unexpectedly: {ex.Message}");
            }
            finally
            {
                driver.Quit();
            }
        }

        private async Task clickWizardIcon(IWebDriver driver)
        {
            var wizardElement = driver.FindElements(By.ClassName("wizard-icon")).FirstOrDefault();
            if (wizardElement != null)
            {
                var actions = new Actions(driver);
                actions.MoveToElement(wizardElement);
                await Task.Delay(100);
                actions.Click().Perform();
            }
        }
        private async Task ClickButton(IWebDriver driver,  IWebElement button)
        {
            try
            {
                var actions = new Actions(driver);
                actions.MoveToElement(button);
                actions.SendKeys(Keys.Backspace).Perform(); 

                await Task.Delay(500);
                actions.Click().Perform();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Button '{button.Text}' not found. Error: {ex.Message}");
            }
        }

        private async Task<bool> ErrorCaptchaAsync(IWebDriver driver, string buttonText, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            var prefix = $"{CodeMapping.GetSiteIdentifierByKey(buttonText)} {_bezKolejkiService.TruncateText(buttonText, 30)}.";
            const int maxRetryCount = 2;

            var captchaElement = driver.FindElements(By.ClassName("sweet-modal-warning")).FirstOrDefault();
            if (captchaElement == null)
            {
                return false;
            }

            while (retryCount < maxRetryCount && captchaElement != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryCount++;
                _logger.LogInformation($"{prefix} Captcha detected on attempt {retryCount} Refreshing the page");

                //await Task.Delay(2000);
                driver.Navigate().Refresh();
                await Task.Delay(4000);

                captchaElement = driver.FindElements(By.ClassName("sweet-modal-warning")).FirstOrDefault();
                if (captchaElement == null)
                {
                    var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                    if (button != null)
                    {
                        _logger.LogInformation($"{prefix}. Re-clicking button after captcha refresh.");
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                        await Task.Delay(2500);
                        await clickWizardIcon(driver);
                        await Task.Delay(4500);
                        await ClickButton(driver, button);
                        await Task.Delay(4000);
                    }
                }

                captchaElement = driver.FindElements(By.ClassName("sweet-modal-warning")).FirstOrDefault();
                if (captchaElement == null)
                {
                    return false;
                }
            }

            _logger.LogError($"{prefix} Exceeded maximum retry attempts ({maxRetryCount}) for captcha resolution.");
            driver.Navigate().Refresh();
            await Task.Delay(4000);
            return true;
        }
    }
}
