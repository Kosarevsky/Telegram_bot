﻿using Data.Context;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Repositories
{
    public class ExecutionRepository : IExecutionRepository
    {
        private readonly BotContext _context;
        public ExecutionRepository(BotContext context)
        {
            _context = context;
        }

        public IQueryable<Execution> GetAll()
        {
            return _context.Execution
                .Include(d => d.AvailableDates)
                .AsNoTracking();
        }

        public IQueryable<Execution> GetAll(Expression<Func<Execution, bool>> predicate)
        {
            return _context.Execution
                .Include(d => d.AvailableDates)
                .Where(predicate)
                .AsNoTracking();
        }

        public async Task SaveOperationWithDatesAsync(Execution op)
        {
            op.ExecutionDateTime = await _context.GetCurrentDateTimeFromServerAsync();
            var existingExecution = await _context.Execution
                .Include(e => e.AvailableDates)
                .FirstOrDefaultAsync(e => e.Code == op.Code && e.ExecutionDateTime==op.ExecutionDateTime);

            if (existingExecution != null)
            {
                _context.AvailableDates.RemoveRange(existingExecution.AvailableDates);

                _context.Entry(existingExecution).State = EntityState.Detached;
                existingExecution.AvailableDates = op.AvailableDates;
                existingExecution.ExecutionDateTime = op.ExecutionDateTime;
                _context.Attach(existingExecution);
            }
            else
            {
                _context.Execution.Add(op);
            }

            await _context.SaveChangesAsync();
        }
    }
}
