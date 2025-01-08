using Data.Entities;
using Services.Models;
using System.Linq.Expressions;

namespace Services.Interfaces
{
    public interface IClientService
    {
        Task<List<ClientModel>> GetAllAsync(Expression<Func<Client, bool>>? predicate = null);
    }
}
