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

        public IQueryable<Client> GetAllAsync()
        {
            return _context.Client.AsNoTracking();
        }

        public IQueryable<Client> GetAllAsync(Expression<Func<Client, bool>> predicate)
        {
            return _context.Client
                .AsNoTracking()
                .Where(predicate);
        }
        public async Task SaveAsync(Client client)
        {
            try
            {
                var result = await _context.Client
                    .FirstOrDefaultAsync(u => u.Id == client.Id);

                if (result != null)
                {
                    _context.Entry(result).CurrentValues.SetValues(client);
                }
                else
                {
                    await _context.Client.AddAsync(client);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
