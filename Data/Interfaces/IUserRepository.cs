using Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace Data.Interfaces
{
    public interface IUserRepository 
    {
        IQueryable<User> GetAllAsync();
        IQueryable<User> GetAllAsync(Expression<Func<User, bool>> predicate);
        Task SaveSubscriptionAsync(long telegramId, string code);
        Task UpdateLastNotificationDateAsync(User user);
        Task DeleteSubscriptionAsync(long telegramId, string code);
    }
}
