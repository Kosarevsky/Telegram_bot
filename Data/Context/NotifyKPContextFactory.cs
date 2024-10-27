using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Data.Context
{
    public class NotifyKPContextFactory : IDesignTimeDbContextFactory<NotifyKPContext>
    {
        public NotifyKPContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<NotifyKPContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new NotifyKPContext(optionsBuilder.Options);
        }
    }
}
