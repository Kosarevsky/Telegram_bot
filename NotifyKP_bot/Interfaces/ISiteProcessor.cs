namespace BezKolejki_bot.Interfaces
{
    public interface ISiteProcessor
    {
        Task ProcessSiteAsync(string url, CancellationToken cancellationToken);
    }
}
