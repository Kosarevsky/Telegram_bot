namespace Data.Interfaces
{
    public interface IUnitOfWork
    {
        IExecutionRepository Executions { get; }
        IUserRepository User { get; }
        IUserSubscriptionRepository UserSubscription { get; }
        IExecutionRepository ExecutionRepository { get; }

        Task<DateTime> GetCurrentDateTimeFromSQLServer();
        void Save();
    }
}
