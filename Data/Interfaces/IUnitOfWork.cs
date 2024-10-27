using System;
namespace Data.Interfaces
{
    public interface IUnitOfWork
    {
        IOperationRepository Operations { get; }

        void Save();
    }
}
