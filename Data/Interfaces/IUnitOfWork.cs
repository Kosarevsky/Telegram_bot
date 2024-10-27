using System;
namespace Data.Interfaces
{
    public interface IUnitOfWork
    {
        IOperationRepository Operations { get; }

        Task<DateTime> GetCurrentDateTimeFromSQLServer();
        void Save();
    }
}
