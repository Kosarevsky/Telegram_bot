using Services.Models;

namespace Services.Interfaces
{
    public interface IBezKolejkiService
    {
        Task SaveAsync(string code, List<DateTime> dates);
        Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code);
        Task<List<UserModel>> GetActiveUsers();
        Task SaveDatesToDatabase(List<DateTime> dates, List<DateTime> previousDates, string code);
        string TruncateText(string text, int maxLength);
    }
}