using Data.Entities;
using System.Linq.Expressions;

namespace Data.Interfaces
{
    public interface IClientRepository
    {
        IQueryable<Client> GetAll();

        IQueryable<Client> GetAll(Expression<Func<Client, bool>> predicate);
    }
}
