using Data.Entities;
using System.Linq.Expressions;
namespace Data.Interfaces
{
    public interface IUserRepository 
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<IEnumerable<User>> GetAllAsync(Expression<Func<User, bool>> predicate);
        Task SaveSubscriptionAsync(long telegramId, string code, List<DateTime>? dates);
        Task DeleteSubscriptionAsync(long telegramId, string code);
    }
}
