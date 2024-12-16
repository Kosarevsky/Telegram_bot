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

namespace BezKolejki_bot.Services
{
    public class BrowserSiteProcessor : ISiteProcessor
    {
        private readonly ILogger<BrowserSiteProcessor> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisherService _eventPublisherService;
        private readonly int _interval;
        private readonly ConcurrentDictionary<string, bool> _processingSite = new();
        private readonly Dictionary<string, ISiteProcessor> _siteProcessor = new();

        public BrowserSiteProcessor(ILogger<BrowserSiteProcessor> logger, IBezKolejkiService bezKolejkiService, IConfiguration configuration, IEventPublisherService eventPublisherService)
        {
            _logger = logger;
            _bezKolejkiService = bezKolejkiService;
            _configuration = configuration;
            _eventPublisherService = eventPublisherService;
            var timeOutBrowser = configuration["ScheduledTask:TimeOutBrowser"];
            if (!int.TryParse(timeOutBrowser, out _interval) || _interval <= 0)
            {
                throw new InvalidOperationException("Timeout Task Scheduled interval is not properly configured.");
            }
        }

        public Task ProcessSiteAsync(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task GetAvailableDateAsync(IEnumerable<string> urls)
        {
            var tasks = urls.Select(url => ProcessSiteAsync(url)).ToList();
            await Task.WhenAll(tasks);
        }
        private async Task ProcessSiteAsync(string url) 
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
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddExcludedArgument("enable-automation");
                options.AddArgument("--no-sandbox");
                options.AddArgument("start-maximized");
                options.AddArgument("disable-infobars");
                options.AddArgument("--disable-dev-shm-usage");
                //options.AddArgument("--disable-gpu");
                options.AddArgument("--enable-unsafe-swiftshader");
                options.AddArgument("--window-size=1920x1080");

                options.AddAdditionalOption("useAutomationExtension", false);
                options.AddArgument("user-agent='Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36'");
                options.AddArgument("--headless");
                options.AddArgument("--incognito");

                var cancellationTokenSource = new CancellationTokenSource();
                var timer = new Timer(state =>
                {
                    _logger.LogWarning("Browser automation timed out. Closing the browser.");
                    cancellationTokenSource.Cancel();
                }, null, TimeSpan.FromSeconds(_interval), Timeout.InfiniteTimeSpan);

                try
                {
                    using (var driver = new ChromeDriver(options))
                    {
                        driver.Navigate().GoToUrl(url);
                        await Task.Delay(1500);

                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        await ExecuteBrowserAutomation(driver, wait, cancellationTokenSource.Token);
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

        private async Task ExecuteBrowserAutomation(IWebDriver driver, WebDriverWait wait, CancellationToken cancellationToken)
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
                    var buttonText = buttonTexts[index];
                    var buttonCode = CodeMapping.GetValueByKey(buttonText);
                    var countUserBySubscribe = activeUsers
                        .Where(user => user.Subscriptions.Any(sub => sub.SubscriptionCode == buttonCode))
                        .Count();
                    if (countUserBySubscribe <= 0)
                    {
                        _logger.LogInformation($"{buttonCode} count subscribers = 0. skipping....");
                        index++;
                        continue;
                    }
                    else
                    {
                        _logger.LogInformation($"{buttonCode} count subscribers has {countUserBySubscribe}");
                    }


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
                                    var data = JsonConvert.DeserializeObject<BezKolejkiJsonModel>(responseBody);

                                    if (data != null)
                                    {
                                        var code = CodeMapping.GetCodeByOperationId(data.operationId);
                                        if (string.IsNullOrEmpty(code))
                                        {
                                            _logger.LogWarning($"Error mapping code {data.operationId}");
                                        }
                                        var previousDates = new List<DateTime>();
                                        try
                                        {
                                            previousDates = await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(code);
                                        }
                                        catch (Exception)
                                        {
                                            _logger.LogWarning($"Error loading previousDates {data.operationId}");
                                        }

                                        var availableDates = new List<DateTime>();

                                        foreach (var dateStr in data.availableDays)
                                        {
                                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                                            {
                                                availableDates.Add(parsedDate);
                                            }
                                            else
                                            {
                                                _logger.LogWarning($"Error parse string '{dateStr}' to DateTime");
                                            }
                                        }

                                        if ((availableDates.Any() || previousDates.Any()) && !dataSaved)
                                        {
                                            await SaveDatesToDatabase(availableDates, previousDates, code);
                                            dataSaved = true;

                                        }
                                        else if (!availableDates.Any())
                                        {
                                            _logger.LogInformation($"{buttonText}. Not available date for save");
                                        }
                                    }
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
                                await Task.Delay(1000);

                                button.Click();


                                await Task.Delay(1000);
                                wait.Until(d =>
                                {
                                    var loadingElement = driver.FindElement(By.CssSelector(".vld-overlay.is-active"));
                                    return loadingElement.GetCssValue("display") == "none";
                                });

                                await ErrorCaptchaAsync(driver, buttonText);
                                index++;
                                success = true;
                                //continue;
                            }
                            catch (StaleElementReferenceException ex)
                            {
                                _logger.LogInformation($"{buttonText}.Stale element detected. Retrying for current button. {ex.Message}");
                                await Task.Delay(1000);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning($"{buttonText}.Error event");
                        throw;
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
                throw;
            }
            finally
            {
                driver.Quit();
            }
        }

        private async Task<bool> ErrorCaptchaAsync(IWebDriver driver, string buttonText)
        {
            const int maxRetryCount = 2;
            int retryCount = 0;

            var prefix = $"{CodeMapping.GetSiteIdentifierByKey(buttonText)} {TruncateText(buttonText,30)}.";

            while (retryCount < maxRetryCount)
            {
                var captchaElement = driver.FindElements(By.ClassName("sweet-modal-warning")).FirstOrDefault();
                if (captchaElement == null)
                {
                    return false;
                }

                retryCount++;
                _logger.LogInformation($"{prefix} Captcha detected on attempt {retryCount} Refreshing the page");

                await Task.Delay(2000);
                driver.Navigate().Refresh();
                await Task.Delay(4000);

                captchaElement = driver.FindElements(By.ClassName("sweet-modal-warning")).FirstOrDefault();

                if (captchaElement != null)
                {
                    continue;
                }

                var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                if (button != null)
                {
                    _logger.LogInformation($"{prefix}. Re-clicking button after captcha refresh.");
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                    await Task.Delay(500);
                    button.Click();
                    await Task.Delay(1000);
                }
            }
            _logger.LogError($"{prefix} Exceeded maximum retry attempts ({maxRetryCount}) for captcha resolution.");
            driver.Navigate().Refresh();
            await Task.Delay(4000);
            return true;
        }
        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;

            return text.Length > maxLength
                ? text.Substring(0, maxLength - 3) + "..."
                : text;
        }

        private async Task SaveDatesToDatabase(List<DateTime> dates, List<DateTime> previousDates, string code)
        {
            var buttonName = TruncateText(CodeMapping.GetKeyByCode(code),60);
            if (dates != null && (dates.Any() || previousDates.Any()))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    try
                    {
                        await _bezKolejkiService.SaveAsync(code, dates); 
                        _logger.LogInformation($"Save date to {code}, {buttonName}");
                        await _eventPublisherService.PublishDatesSavedAsync(code, dates, previousDates);
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning($"Error save or published data {code}");
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning($"No code found for button ({code} {buttonName})");
                }
            }
            else
            {
                _logger.LogInformation($"No dates available to save ({code} {buttonName})");
            }
        }


    }
}
