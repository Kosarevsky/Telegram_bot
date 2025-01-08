using AutoMapper;
using Data.Entities;
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
                cfg.CreateMap<Client, ClientModel>()
                    .ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        public async Task<List<ClientModel>> GetAllAsync(Expression<Func<Client, bool>>? predicate = null)
        {
            var clients = predicate == null
                ? await _database.Client.GetAll().ToListAsync() 
                : await _database.Client.GetAll(predicate).ToListAsync();

            return _mapper.Map<List<ClientModel>>(clients);
        }
    }
}
