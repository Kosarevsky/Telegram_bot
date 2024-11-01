namespace Data.Interfaces
{
    public interface IUnitOfWork
    {
        IExecutionRepository Executions { get; }

        Task<DateTime> GetCurrentDateTimeFromSQLServer();
        void Save();
    }
}
