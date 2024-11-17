using Data.Entities;
using Services.Models;
using System.Linq.Expressions;

namespace Services.Interfaces
{
    public interface IBezKolejkiService
    {
        Task SaveAsync(string code, List<DateTime> dates);
        Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code);
    }
}