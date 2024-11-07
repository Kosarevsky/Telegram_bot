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
            var user = await _context.Users
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);
            if (user == null)
            {
                user = new User { TelegramUserId = telegramId };
                _context.Users.Add(user);
            }

            var subscription = user.Subscriptions.FirstOrDefault(s => s.SubscriptionCode == code);

            if (subscription == null)
            {
                subscription = new UserSubscription {
                    SubscriptionCode = code,
                    UserId = user.Id,
                    SubscriptionDateTime = await _context.GetCurrentDateTimeFromServerAsync()
                };
                user.Subscriptions.Add(subscription);
            }

            await _context.SaveChangesAsync();
        }
        public async Task DeleteSubscriptionAsync(long telegramId, string code)
        {
            var user = await _context.Users
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);
            if (user != null)
            {
                var userSubscriptions = user.Subscriptions.FirstOrDefault(s => s.SubscriptionCode == code);
                if (userSubscriptions != null)
                {
                    user.Subscriptions.Remove(userSubscriptions);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
