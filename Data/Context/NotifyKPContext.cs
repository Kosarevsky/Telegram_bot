using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    public class NotifyKPContext : DbContext
    {
        public NotifyKPContext(DbContextOptions<NotifyKPContext> options) : base(options) { }

        public DbSet<OperationRecord> OperationRecords { get; set; }
        public DbSet<DateRecord> DateRecords { get; set; }


        public async Task<DateTime> GetCurrentDateTimeFromServerAsync()
        {
            var currentDate = await this.DateRecords
                .FromSqlRaw("SELECT GETDATE() AS CurrentDate")
                .AsNoTracking()
                .Select(d => d.Date)
                .FirstOrDefaultAsync();

            return currentDate;
        }

    }
}
