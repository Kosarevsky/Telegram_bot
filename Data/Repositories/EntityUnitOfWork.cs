using Data.Context;
using Data.Interfaces;

namespace Data.Repositories
{
    public class EntityUnitOfWork : IUnitOfWork
    {
        private readonly BotContext _context;
        private ExecutionRepository _executionRepository;
        private UserRepository _userRepository;
        private UserSubscriptionRepository _userSubscriptionRepository;
        public IExecutionRepository Executions =>
            _executionRepository ?? (_executionRepository = new ExecutionRepository(_context));
        public IUserRepository User =>
            _userRepository ?? (_userRepository = new UserRepository(_context));
        public IUserSubscriptionRepository UserSubscription =>
            _userSubscriptionRepository ?? (_userSubscriptionRepository = new UserSubscriptionRepository(_context));
        public IExecutionRepository ExecutionRepository =>
            _executionRepository ?? (_executionRepository = new ExecutionRepository(_context));

        public async Task<DateTime> GetCurrentDateTimeFromSQLServer() => await _context.GetCurrentDateTimeFromServerAsync();

        public EntityUnitOfWork(BotContext context)
        {
            _context = context;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
