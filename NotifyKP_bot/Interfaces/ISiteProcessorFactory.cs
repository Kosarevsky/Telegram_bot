using BezKolejki_bot.Models;

namespace BezKolejki_bot.Interfaces
{
    public interface ISiteProcessorFactory
    {
        SiteProcessorResult GetProcessor(string url);
    }
}
