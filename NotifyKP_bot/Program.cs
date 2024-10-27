using NotifyKP_bot.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NotifyKP_bot
{
    public class Program
    {
        static async Task Main()
        {

            var browserService = new BrowserAutomationService();
            try
            {
                List<DateTime> dates = await browserService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
            }
            Console.ReadLine();
        }
    }
}
