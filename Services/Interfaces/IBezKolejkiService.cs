namespace Services.Interfaces
{
    public interface IBezKolejkiService
    {
        Task SaveAsync(List<DateTime> dates, string code);
    }
}
