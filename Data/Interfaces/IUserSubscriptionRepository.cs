namespace Data.Interfaces
{
    public interface IUserSubscriptionRepository
    {
        Task SaveUserSubscriptionAsync(long telegramUserId, string code);
    }
}
