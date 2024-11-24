using Data.Entities;
using System.Linq.Expressions;
namespace Data.Interfaces
{
    public interface IUserRepository 
    {
        IQueryable<User> GetAllAsync();
        IQueryable<User> GetAllAsync(Expression<Func<User, bool>> predicate);
        Task SaveSubscriptionAsync(long telegramId, string code);
        Task UpdateLastNotificationDateAsync(User userTg);
        Task DeleteSubscriptionAsync(long telegramId, string code);
        void DeactivateUserAsync(User user);
    }
}
