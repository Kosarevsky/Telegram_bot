using Data.Context;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly BotContext _notifyKPContext;

        public UserSubscriptionRepository(BotContext notifyKPContext)
        {
            _notifyKPContext = notifyKPContext;
        }

        public async Task SaveUserSubscriptionAsync(long telegramUserId, string code)
        {
            var user = await _notifyKPContext.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);
            if (user == null)
            {
                user = new User { TelegramUserId = telegramUserId, Subscriptions = new List<UserSubscription>() };
                _notifyKPContext.Users.Add(user);
            }


        }
    }
}
