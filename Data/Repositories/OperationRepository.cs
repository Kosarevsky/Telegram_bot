using Data.Context;
using Data.Entities;
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

        public async Task SaveOperationWithDatesAsync(OperationRecord op)
        {
            if (op == null)
            {
                throw new ArgumentNullException(nameof(op), "Operation record cannot be null");
            }

            // Добавляем основную запись
            await _context.OperationRecords.AddAsync(op);

            // Если у вас есть связанные записи, добавьте их
            if (op.DateRecords != null && op.DateRecords.Any())
            {
                // Убедитесь, что записи также добавлены в контекст
                await _context.DateRecords.AddRangeAsync(op.DateRecords);
            }

            // Сохраняем изменения
            await _context.SaveChangesAsync();
        }
    }
}
