using Data.Entities;
using Services.Models;
using System.Linq.Expressions;

namespace Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserModel>> GetAllAsync(Expression<Func<User, bool>>? predicate = null);
        Task<UserModel> GetByTelegramId(long telegramId);
        Task SaveSubscription(long telegramId, string code);
        Task DeleteSubscription(long telegramId, string code);
        Task UpdateLastNotificationDateAsync(UserModel tgUser);
        void DeactivateUserAsync(long chatId);
    }
}
