namespace Data.Interfaces
{
    public interface IUnitOfWork
    {
        IExecutionRepository Executions { get; }
        IUserRepository User { get; }
        IUserSubscriptionRepository UserSubscription { get; }
        Task<DateTime> GetCurrentDateTimeFromSQLServer();
        Task SaveAsync();
    }
}
