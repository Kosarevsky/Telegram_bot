using Data.Context;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BotContext _context;
        public UserRepository(BotContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAsync()
        {
            return await _context.Users
                .Include(s => s.Subscriptions)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(long telegramId)
        {
            return await _context.Users
                .Include(s => s.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);
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
                await _context.SaveChangesAsync(); 
            }

            if (!user.Subscriptions.Any(s => s.SubscriptionCode == code))
            {
                var subscription = new UserSubscription { SubscriptionCode = code, UserId = user.Id };
                user.Subscriptions.Add(subscription); 
                await _context.SaveChangesAsync(); 
            }
        }


        public async Task DeleteSubscriptionAsync(long telegramId, string code)
        {
            var user = await _context.Users
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);
            if (user != null) {

                var userSubscriptions = user.Subscriptions.FirstOrDefault(s => s.SubscriptionCode == code);
                if (userSubscriptions != null) {
                    user.Subscriptions.Remove(userSubscriptions);
                    await _context.SaveChangesAsync();
                }

            }
        }

    }
}
