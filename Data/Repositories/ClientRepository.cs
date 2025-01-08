using Data.Entities;
using Data.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly BotContext _context;

        public ClientRepository(BotContext context)
        {
            _context = context;
        }

        public IQueryable<Client> GetAll()
        {
            return _context.Client.AsNoTracking();
        }

        public IQueryable<Client> GetAll(Expression<Func<Client, bool>> predicate)
        {
            return _context.Client
                .AsNoTracking()
                .Where(predicate);
        }
    }
}
