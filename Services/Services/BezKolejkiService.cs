using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class BezKolejkiService : IBezKolejkiService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;

        public BezKolejkiService(IUnitOfWork database)
        {
            _database = database;
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Data.Entities.User, UserModel>()
                    .ForMember(dest => dest.Subscriptions, opt => opt.MapFrom(src => src.Subscriptions))
                    .ReverseMap();

                cfg.CreateMap<UserSubscription, UserSubscriptionModel>()
                    .ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        public async Task SaveAsync(string code, List<DateTime> dates)
        {
            var ex = new Execution
            {
                Code = code,
                AvailableDates = dates.Select(d=> new AvailableDate { Date = d}).ToList(),
            };
            await _database.Executions.SaveOperationWithDatesAsync(ex);
        }

        public async Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code) 
        {
            var executions = await _database.Executions
                .GetAll(d => d.Code == code)
                .OrderByDescending(d => d.ExecutionDateTime)
                .FirstOrDefaultAsync();
            var dates = executions?.AvailableDates.Select(d => d.Date).ToList();
            return dates ?? new List<DateTime>();
        }

        public async Task<List<UserModel>> GetActiveUsers()
        {
            var activeUsers = await _database.User.GetAllAsync(u => u.IsActive && u.Subscriptions.Any()).ToListAsync();
            return _mapper.Map<List<UserModel>>(activeUsers);
        }
    }
}
