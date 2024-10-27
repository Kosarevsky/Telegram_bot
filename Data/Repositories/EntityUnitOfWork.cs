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

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
