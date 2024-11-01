using Data.Context;
using Data.Interfaces;

namespace Data.Repositories
{
    public class EntityUnitOfWork : IUnitOfWork
    {
        private readonly NotifyKPContext _context;
        private ExecutionRepository _executionRepository;
        public IExecutionRepository Executions =>
            _executionRepository ?? (_executionRepository = new ExecutionRepository(_context));

        public async Task<DateTime> GetCurrentDateTimeFromSQLServer() => await _context.GetCurrentDateTimeFromServerAsync();

        public EntityUnitOfWork(NotifyKPContext context)
        {
            _context = context;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
