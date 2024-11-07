using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BezKolejki_bot.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using Services.Models;
using System.Text.RegularExpressions;

namespace BezKolejki_bot.Services
{
    public class BrowserAutomationService : IBrowserAutomationService
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IEventPublisherService _eventPublisher;
        private readonly IConfiguration _configuration;
        private readonly int _interval;

        public BrowserAutomationService(ILogger<BrowserAutomationService> logger, IBezKolejkiService bezKolejkiService, IEventPublisherService eventPublisher, IConfiguration configuration)
        {
            _logger = logger;
            _bezKolejkiService = bezKolejkiService;
            _eventPublisher = eventPublisher;
            _configuration = configuration;
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
                while (index < buttonTexts.Count)
                {
                    var buttonText = buttonTexts[index];
                    bool success = false;
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

                            await Task.Delay(500);
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                            await Task.Delay(1000);
                            button.Click();

                            await Task.Delay(1000);
                            wait.Until(d =>
                            {
                                var loadingElement = driver.FindElement(By.CssSelector(".vld-overlay.is-active"));
                                return loadingElement.GetCssValue("display") == "none";
                            });

                            await ErrorCaptchaAsync(driver, button.Text, 5);

                            var buttonDates = CollectAvailableDates(driver, wait);

                            var siteName = driver.FindElement(By.CssSelector(".navbar-title")).Text;

                           await SaveDatesToDatabase(buttonDates, button.Text, siteName);

                            success = true;
                        }
                        catch (StaleElementReferenceException ex)
                        {
                            _logger.LogInformation($"Stale element detected. Retrying for current button. {ex.Message}");
                            await Task.Delay(1000);
                        }
                    }
                    index++;
                }
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

        private async Task ErrorCaptchaAsync(IWebDriver driver, string buttonText, int maxAttempts)
        {
            var attempt = 0;
            while (attempt < maxAttempts)
            {
                if (!driver.FindElements(By.ClassName("sweet-modal-warning")).Any())
                {
                    return;
                }

                attempt++;
                _logger.LogInformation($"Captcha error attempt: {attempt}");

                driver.Navigate().Refresh();
                await Task.Delay(3000 * attempt);

                var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                if (button == null)
                {
                    _logger.LogWarning($"Button with text '{buttonText}' not found after refresh.");
                    continue;
                }
                await Task.Delay(1000 * attempt);
            }

            _logger.LogError("Failed to bypass captcha after maximum attempts.");
        }

        private async Task SaveDatesToDatabase(List<DateTime> dates, string buttonName, string siteName)
        {
            if (dates != null && dates.Any())
            {
                var code = CodeMapping.GetValueByKey(buttonName);
                if (!string.IsNullOrEmpty(code))
                {
                    var sendedDates = await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(code);


                    await _bezKolejkiService.SaveAsync(code, dates); 
                    _logger.LogInformation($"Save date to {code}, {code}");
                    await _eventPublisher.PublishDatesSavedAsync(code, dates, sendedDates);
                    _logger.LogInformation("Subscribed to DatesSaved event.");
                }
                else
                {
                    _logger.LogInformation($"No code found for button ({buttonName})");
                }
            }
            else
            {
                _logger.LogInformation($"No dates available to save ({buttonName})");
            }
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
            catch (WebDriverTimeoutException )
            {
                _logger.LogInformation("Dates not found");
            }

            return dates;
        }

        static string ExtractDateFromClass(string classAttribute)
        {
            var match = Regex.Match(classAttribute, @"id-(\d{4}-\d{2}-\d{2})");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
