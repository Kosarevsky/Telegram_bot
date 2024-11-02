using Microsoft.Extensions.Logging;
using NotifyKP_bot.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using Services.Models;
using System.Text.RegularExpressions;


namespace NotifyKP_bot.Services
{
    public class BrowserAutomationService : IBrowserAutomationService
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly IBialaService _bialaService;
        private readonly IEventPublisher _eventPublisher;

        public BrowserAutomationService(ILogger<BrowserAutomationService> logger, IBialaService bialaService, IEventPublisher eventPublisher)
        {
            _logger = logger;
            _bialaService = bialaService;
            _eventPublisher = eventPublisher;
        }
        public async Task GetAvailableDateAsync(string url)
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless");  
            //options.AddArgument("--auto-open-devtools-for-tabs");
            var dates = new List<DateTime>();

            using (var driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl(url); //https://bezkolejki.eu/luwbb/

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                await Task.Delay(1500);
                try
                {
                    var listOperacja2 = wait.Until(d => d.FindElement(By.Id("Operacja2")));
                    var btn = listOperacja2.FindElements(By.TagName("button")).FirstOrDefault();
                    btn?.Click();

                    await ErrorCaptchaAsync(driver, btn.Text, 5);
                    wait.Until(d => d.FindElement(By.Id("Operacja2")).FindElements(By.TagName("button")).Count > 0);

                    listOperacja2 = wait.Until(d => d.FindElement(By.Id("Operacja2")));
                    var buttons = listOperacja2.FindElements(By.TagName("button"));

                    foreach (var button in buttons)
                    {
                        //await Task.Delay(1500);
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                        await Task.Delay(500);
                        button.Click();
                        //wait.Until(d => d.FindElement(By.ClassName("loading-complete-indicator"))); // замените на класс или элемент, который появляется после загрузки
                        await Task.Delay(500);
                        wait.Until(d =>
                        {
                            var loadingElement = driver.FindElement(By.CssSelector(".vld-overlay.is-active"));
                            return loadingElement.GetCssValue("display") == "none";
                        });

                        await ErrorCaptchaAsync(driver, button.Text, 5);
                        var buttonDates = CollectAvailableDates(driver, wait);
                        SaveDatesToDatabase(buttonDates, button.Text);
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    _logger.LogInformation("Error: element not found or timeout");
                    throw;
                }

                finally {
                    driver.Quit();
                }
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
                await Task.Delay(1000 * attempt);

                var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                if (button == null)
                {
                    _logger.LogWarning($"Button with text '{buttonText}' not found after refresh.");
                    continue; 
                }

                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                await Task.Delay(1300);
                button.Click();
                await Task.Delay(1000 * attempt); 
            }

            _logger.LogError("Failed to bypass captcha after maximum attempts.");
            //throw new InvalidOperationException("Captcha bypass failed after maximum attempts.");
        }

        private void SaveDatesToDatabase(List<DateTime> dates, string buttonName)
        {
            if (dates != null && dates.Any())
            {
                if (BialaCodeMapping.buttonCodeMapping.TryGetValue(buttonName, out var buttonCode))
                {
                    _bialaService.Save(dates, buttonCode); 
                    _logger.LogInformation($"Save date to {buttonName}, {buttonCode}");
                    _ = _eventPublisher.PublishDatesSavedAsync(buttonCode, dates);
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
