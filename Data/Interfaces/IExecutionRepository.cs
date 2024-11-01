using Data.Entities;

namespace Data.Interfaces
{
    public interface IExecutionRepository
    {
        Task SaveOperationWithDatesAsync(Execution op);
    }
}
