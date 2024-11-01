using Data.Entities;
namespace Data.Interfaces
{
    public interface IUserRepository 
    {
        Task<List<User>> GetAsync();
        Task<User?> GetByIdAsync(long telegramId);
        Task SaveSubscriptionAsync(long telegramId, string code);
        Task DeleteSubscriptionAsync(long telegramId, string code);
    }
}
