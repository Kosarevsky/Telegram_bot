using Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Context
{
    public class NotifyKPContext : DbContext
    {
        public NotifyKPContext(DbContextOptions<NotifyKPContext> options) : base(options) { }

        public DbSet<Execution> Execution { get; set; }
        public DbSet<AvailableDate> AvailableDates { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        public async Task<DateTime> GetCurrentDateTimeFromServerAsync()
        {
            var result = await this.Set<CurrentDateTimeDto>()
                .FromSqlRaw("SELECT CAST(GETDATE() AS DATETIME) AS CurrentDateTime")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return result?.CurrentDateTime ?? DateTime.Now;
        }

        [Keyless]
        [NotMapped]
        public class CurrentDateTimeDto
        {
            public DateTime CurrentDateTime { get; set; }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CurrentDateTimeDto>().HasNoKey();

            modelBuilder.Entity<User>()
            .HasMany(u => u.Subscriptions)
            .WithOne(uc => uc.User)
            .HasForeignKey(uc => uc.UserId);
        }
    }
}
