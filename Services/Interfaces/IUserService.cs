using Services.Models;

namespace Services.Interfaces
{
    public interface IUserService
    {
        Task<UserModel?> GetByTelegramIdAsync(long telegramId); 
        Task<List<UserModel>> GetAllAsync();
        Task SaveSubscription(long telegramId, string code);
        Task DeleteSubsription(long telegramId, string code);
    }
}
