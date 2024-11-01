using Data.Context;
using Data.Entities;
using Data.Interfaces;

namespace Data.Repositories
{
    public class ExecutionRepository : IExecutionRepository
    {
        private readonly BotContext _context;
        public ExecutionRepository(BotContext context)
        {
            _context = context;
        }

        public async Task SaveOperationWithDatesAsync(Execution op)
        {
            if (op == null)
            {
                throw new ArgumentNullException(nameof(op), "Operation record cannot be null");
            }

            await _context.Execution.AddAsync(op);
            //await _context.AvailableDates.AddRangeAsync(op.AvailableDates);
            await _context.SaveChangesAsync();
        }
    }
}
