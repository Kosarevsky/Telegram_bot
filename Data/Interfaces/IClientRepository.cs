using Data.Entities;
using System.Linq.Expressions;

namespace Data.Interfaces
{
    public interface IClientRepository
    {
        IQueryable<Client> GetAllAsync();

        IQueryable<Client> GetAllAsync(Expression<Func<Client, bool>> predicate);

        Task SaveAsync(Client client);
    }
}
