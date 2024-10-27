using Data.Context;
using Data.Interfaces;

namespace Data.Repositories
{
    public class EntityUnitOfWork : IUnitOfWork
    {
        private readonly NotifyKPContext _context;
        private OperationRepository _operationRepository;
        public IOperationRepository Operations => 
            _operationRepository ?? (_operationRepository = new OperationRepository(_context));

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
