using Data.Context;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BotContext _context;
        public UserRepository(BotContext context)
        {
            _context = context;
        }

        public IQueryable<User> GetAllAsync()
        {
            return _context.Users
                .Include(a => a.Subscriptions)
                .AsNoTracking();
        }

        public IQueryable<User> GetAllAsync(Expression<Func<User, bool>> predicate)
        {
            return _context.Users
                .Include(a => a.Subscriptions)
                .AsNoTracking()
                .Where(predicate);
        }
        public async Task SaveSubscriptionAsync(long telegramId, string code)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var user = await _context.Users
                        .Include(s => s.Subscriptions)
                        .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);

                    var date = await _context.GetCurrentDateTimeFromServerAsync();

                    if (user == null)
                    {
                        user = new User
                        {
                            TelegramUserId = telegramId,
                            DateLastSubscription = date,
                            IsActive = true,
                            Subscriptions = new List<UserSubscription>()
                        };
                        _context.Users.Add(user);
                    }
                    else
                    {
                        user.DateLastSubscription = date;
                        user.IsActive = true;
                        _context.Entry(user).State = EntityState.Modified;
                    }

                    var subscription = user.Subscriptions.FirstOrDefault(s => s.SubscriptionCode == code);

                    if (subscription == null)
                    {
                        user.Subscriptions.Add(new UserSubscription
                        {
                            SubscriptionCode = code,
                            UserId = user.Id,
                            SubscriptionDateTime = date
                        });
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        public async Task UpdateLastNotificationDateAsync(User user)
        {
            user.DateLastSubscription = await _context.GetCurrentDateTimeFromServerAsync();
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubscriptionAsync(long telegramId, string code)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var user = await _context.Users
                       .Include(s => s.Subscriptions)
                       .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);

                    if (user != null)
                    {
                        var userSubscription = user.Subscriptions.FirstOrDefault(s => s.SubscriptionCode == code);
                        if (userSubscription != null)
                        {
                            _context.UserSubscriptions.Remove(userSubscription);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
