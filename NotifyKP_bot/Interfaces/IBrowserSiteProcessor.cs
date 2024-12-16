
namespace BezKolejki_bot.Interfaces
{
    public interface IBrowserSiteProcessor
    {
        Task GetAvailableDateAsync(IEnumerable<string> url);
    }
}

