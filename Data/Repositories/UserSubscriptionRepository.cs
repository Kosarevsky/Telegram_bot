using Data.Context;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly BotContext _context;

        public UserSubscriptionRepository(BotContext notifyKPContext)
        {
            _context = notifyKPContext;
        }

        public async Task SaveUserSubscriptionAsync(long telegramUserId, string code)
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);
            if (user == null)
            {
                user = new User { TelegramUserId = telegramUserId, Subscriptions = new List<UserSubscription>() };
                _context.Users.Add(user);
            }
        }
    }
}
