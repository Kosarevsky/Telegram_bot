using Data.Entities;

namespace Data.Interfaces
{
    public interface IOperationRepository
    {
        Task SaveOperationWithDatesAsync(OperationRecord op);
    }
}
