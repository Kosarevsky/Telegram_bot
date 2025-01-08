using Data.Context;
using Data.Interfaces;

namespace Data.Repositories
{
    public class EntityUnitOfWork : IUnitOfWork
    {
        private readonly BotContextFactory _contextFactory;

        public EntityUnitOfWork(BotContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IExecutionRepository Executions =>
            new ExecutionRepository(CreateContext());

        public IUserRepository User =>
            new UserRepository(CreateContext());

        public IUserSubscriptionRepository UserSubscription =>
            new UserSubscriptionRepository(CreateContext());

        public IClientRepository Client =>
            new ClientRepository(CreateContext());

        private BotContext CreateContext()
        {
            return _contextFactory.CreateDbContext(Array.Empty<string>());
        }

        public async Task<DateTime> GetCurrentDateTimeFromSQLServer()
        {
            using var context = CreateContext();
            return await context.GetCurrentDateTimeFromServerAsync();
        }

        public async Task SaveAsync()
        {
            using var context = CreateContext();
            await context.SaveChangesAsync();
        }
    }
}
