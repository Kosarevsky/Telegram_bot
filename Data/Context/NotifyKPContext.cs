using Data.Entities;
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
            var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT GETDATE()";
                var result = await command.ExecuteScalarAsync(); 
                return (DateTime)result; 
            }

        }

    }
}
