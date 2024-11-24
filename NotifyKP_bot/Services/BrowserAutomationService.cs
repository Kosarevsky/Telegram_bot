using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BezKolejki_bot.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using Services.Models;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BezKolejki_bot.Services
{
    public class BrowserAutomationService : IBrowserAutomationService
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisherService _eventPublisherService;
        private readonly int _interval;

        public BrowserAutomationService(ILogger<BrowserAutomationService> logger, IBezKolejkiService bezKolejkiService, IConfiguration configuration, IEventPublisherService eventPublisherService)
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
        public async Task GetAvailableDateAsync(List<string> urls)
        {
            var tasks = urls.Select(url => ProcessSiteAsync(url)).ToList();
            await Task.WhenAll(tasks);
        }
        private async Task ProcessSiteAsync(string url) {
            var options = new ChromeOptions();
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
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
                while (index < buttonTexts.Count)
                {
                    var buttonText = buttonTexts[index];
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

                            if (await ErrorCaptchaAsync(driver, buttonText))
                            {
                                continue;
                            };

                            success = true;
                            index++;
                        }
                        catch (StaleElementReferenceException ex)
                        {
                            _logger.LogInformation($"Stale element detected. Retrying for current button. {ex.Message}");
                            await Task.Delay(1000);
                        }
                    }
                    options.NetworkResponseReceived -= networkHandler;
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
            const int maxRetryCount = 3;
            int retryCount = 0;

            while (retryCount < maxRetryCount)
            {
                if (!driver.FindElements(By.ClassName("sweet-modal-warning")).Any())
                {
                    return false;
                }

                _logger.LogInformation($"{CodeMapping.GetSiteIdentifierByKey(buttonText)} Captcha detected on attempt {retryCount + 1} Refreshing the page");

                await Task.Delay(2000);
                driver.Navigate().Refresh();
                await Task.Delay(4000);

                var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                if (button != null)
                {
                    _logger.LogInformation($"Re-clicking button '{buttonText}' after captcha refresh.");
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                    await Task.Delay(500);
                    button.Click();
                    await Task.Delay(1000);
                    return true;
                }
                retryCount++;

            }
            _logger.LogError($"Exceeded maximum retry attempts ({maxRetryCount}) for captcha resolution.");
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

        static string ExtractDateFromClass(string classAttribute)
        {
            var match = Regex.Match(classAttribute, @"id-(\d{4}-\d{2}-\d{2})");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private List<DateTime> CollectAvailableDates(IWebDriver driver, WebDriverWait wait)
        {
            var dates = new List<DateTime>();
            try
            {
                var elements = wait.Until(d => d.FindElements(By.XPath("//div[contains(@class, 'vc-day')]//span[@aria-disabled='false']")));

                foreach (var element in elements)
                {
                    var parentDiv = element.FindElement(By.XPath("./.."));
                    var dateStrFromClass = ExtractDateFromClass(parentDiv.GetAttribute("class"));
                    if (DateTime.TryParse(dateStrFromClass, out DateTime result))
                    {
                        dates.Add(result);
                    }
                }
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogInformation("Dates not found");
            }

            return dates;
        }
    }
}
