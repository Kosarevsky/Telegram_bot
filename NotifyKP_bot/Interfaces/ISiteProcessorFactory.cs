namespace BezKolejki_bot.Interfaces
{
    public interface ISiteProcessorFactory
    {
        ISiteProcessor GetProcessor(string url);
    }
}
