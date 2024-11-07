using Data.Entities;
using System.Linq.Expressions;

namespace Data.Interfaces
{
    public interface IExecutionRepository
    {
        IQueryable<Execution> GetAll();

        IQueryable<Execution> GetAll(Expression<Func<Execution, bool>> predicate);
        Task SaveOperationWithDatesAsync(Execution op);
    }
}
