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
