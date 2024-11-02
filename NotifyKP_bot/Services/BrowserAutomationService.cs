﻿using Data.Context;
using Microsoft.Extensions.Logging;
using NotifyKP_bot.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using System.Text.RegularExpressions;


namespace NotifyKP_bot.Services
{
    public class BrowserAutomationService : IBrowserAutomationService
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly IBialaService _bialaService;

        private readonly Dictionary<string, string> buttonCodeMapping = new Dictionary<string, string>()
        {
            {"Karta Polaka - dorośli", "/Biala01" },
            {"Karta Polaka - dzieci", "/Biala02" },
            {"Pobyt czasowy - wniosek", "/Biala03" },
            {"Pobyt czasowy - braki formalne", "/Biala04" },
            {"Pobyt czasowy - odbiór karty", "/Biala05" },
            {"Pobyt stały i rezydent - wniosek", "/Biala06" },
            {"Pobyt stały i rezydent - braki formalne", "/Biala07" },
            {"Pobyt stały i rezydent - odbiór karty", "/Biala08" },
            {"Obywatele Unii Europejskiej + Polski Dokument Podróży", "/Biala09" }
        };

        public BrowserAutomationService(ILogger<BrowserAutomationService> logger, IBialaService bialaService)
        {
            _logger = logger;
            _bialaService = bialaService;
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
                await Task.Delay(500);
                try
                {
                    var listOperacja2 = wait.Until(d => d.FindElement(By.Id("Operacja2")));
                    var buttons = listOperacja2.FindElements(By.TagName("button"));

                    foreach (var button in buttons)
                    {
                        await Task.Delay(500);
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                        await Task.Delay(500);
                        button.Click();
                        await Task.Delay(1500);
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

        private async Task ErrorCaptchaAsync(IWebDriver driver, string buttonText, int countErrors)
        {
            var err = 0;
            while (err < countErrors && driver.FindElements(By.ClassName("sweet-modal-warning")).Any())
            {
                err++;
                _logger.LogInformation($"Error captcha: {err}");

                driver.Navigate().Refresh();
                await Task.Delay(1000 * err);
                var button = driver.FindElements(By.XPath($"//button[contains(text(), '{buttonText}')]")).FirstOrDefault();
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                await Task.Delay(300);
                button?.Click();
                await Task.Delay(1000 * err);
            }
        }

        private void SaveDatesToDatabase(List<DateTime> dates, string buttonName)
        {
            if (dates != null && dates.Any())
            {
                
                if (buttonCodeMapping.TryGetValue(buttonName, out var buttonCode))
                {
                    _bialaService.Save(dates, buttonCode); 
                    _logger.LogInformation($"Save date to {buttonName}, {buttonCode}");
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
