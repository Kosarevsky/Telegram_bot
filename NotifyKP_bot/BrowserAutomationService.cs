using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;

namespace NotifyKP_bot
{
    public class BrowserAutomationService
    {
        public async Task<List<DateTime>> GetAvailableDateAsync(string url)
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless");  
            //options.AddArgument("--auto-open-devtools-for-tabs");
            var dates = new List<DateTime>();
            var err = 0;

            using (var driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl("https://bezkolejki.eu/luwbb/");
                await Task.Delay(2000);
                var buttons = driver.FindElements(By.XPath("//button[contains(text(), 'Karta Polaka - dzieci')]"));

                if (buttons.Count > 0)
                {
                    var button = buttons.FirstOrDefault();

                    //((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                    //await Task.Delay(1500);
                    button?.Click();

                    await Task.Delay(1000);

                    while (err < 5 && driver.FindElements(By.ClassName("sweet-modal-warning"))?.Count > 0)
                    {
                        err++;

                        Console.WriteLine("Error Capcha");
                        driver.Navigate().Refresh();
                        await Task.Delay(1000 * err);
                        button = driver.FindElements(By.XPath("//button[contains(text(), 'Karta Polaka - dzieci')]")).FirstOrDefault();
                        //((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", button);
                        await Task.Delay(1000 * err);
                        button?.Click();
                    }

                    await Task.Delay(1000);

                    var elements = driver.FindElements(By.XPath("//div[contains(@class, 'vc-day')]//span[@aria-disabled='false']"));

                    Console.WriteLine(elements);

                    foreach (var element in elements)
                    {
                        var parentDiv = element.FindElement(By.XPath("./.."));

                        //Console.WriteLine($"Class attribute: {parentDiv.GetAttribute("class")}");

                        var classAttribute = parentDiv.GetAttribute("class");
                        var dateStrFromClass = ExtractDateFromClass(classAttribute);

                        //Console.WriteLine($"Data from class: {dateStrFromClass}");
                        //Console.WriteLine("----------------------");

                        if (DateTime.TryParse(dateStrFromClass, out DateTime result))
                        {
                            dates.Add(result);
                        }
                    }

                    Console.WriteLine($"All count attributes: {elements.Count}");
                }
                driver.Quit();
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
