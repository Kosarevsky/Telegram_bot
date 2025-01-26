using AutoMapper;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Models;
using System.Linq.Expressions;

namespace Services.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;

        public ClientService(IUnitOfWork database)
        {
            _database = database;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Data.Entities.Client, ClientModel>()
                    .ReverseMap();

            });

            _mapper = config.CreateMapper();
        }

        public async Task<List<ClientModel>> GetAllAsync(Expression<Func<Data.Entities.Client, bool>>? predicate = null)
        {
            var users = predicate == null
                ? await _database.Client.GetAllAsync().ToListAsync() 
                : await _database.Client.GetAllAsync(predicate).ToListAsync();

            return _mapper.Map<List<ClientModel>>(users);
        }

        public async Task SaveAsync(ClientModel client)
        {
            var clientEntity = _mapper.Map<Data.Entities.Client>(client);
            await _database.Client.SaveAsync(clientEntity);
        }
    }
}
