using Data.Context;
using Data.Interfaces;

namespace Data.Repositories
{
    public class OperationRepository : IOperationRepository
    {
        private readonly NotifyKPContext _context;
        public OperationRepository(NotifyKPContext context)
        {
            _context = context;
        }

        public Task SaveOperationWithDatesAsync(IEnumerable<DateTime> dates)
        {
            throw new NotImplementedException();
        }
    }
}
