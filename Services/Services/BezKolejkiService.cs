using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services.Services
{
    public class BezKolejkiService : IBezKolejkiService
    {
        private readonly IUnitOfWork _database;

        public BezKolejkiService(IUnitOfWork database)
        {
            _database = database;
        }

        public async Task SaveAsync(string code, List<DateTime> dates)
        {
            var ex = new Execution
            {
                Code = code,
                AvailableDates = dates.Select(d=> new AvailableDate { Date = d}).ToList(),
            };
            await _database.Executions.SaveOperationWithDatesAsync(ex);
        }

        public async Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code) 
        {
            var executions = await _database.Executions
                .GetAll(d => d.Code == code)
                .OrderByDescending(d => d.ExecutionDateTime)
                .FirstOrDefaultAsync();
            var dates = executions?.AvailableDates.Select(d => d.Date).ToList();
            return dates ?? new List<DateTime>();
        }
    }
}
