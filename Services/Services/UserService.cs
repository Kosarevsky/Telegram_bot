using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Services.Interfaces;
using Services.Models;
using System.Linq.Expressions;


namespace Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;

        public UserService(IUnitOfWork database)
        {
            _database = database;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserModel>()
                    .ForMember(dest => dest.Subscriptions, opt => opt.MapFrom(src => src.Subscriptions))
                    .ReverseMap();

                cfg.CreateMap<UserSubscription, UserSubscriptionModel>()
                    .ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        public async Task<List<UserModel>> GetAllAsync(Expression<Func<User, bool>>? predicate = null)
        {
            var users = predicate == null
                ? _database.User.GetAllAsync() 
                : _database.User.GetAllAsync(predicate);

            return _mapper.Map<List<UserModel>>(users);
        }

        public async Task SaveSubscription(long telegramId, string code)
        {
           await _database.User.SaveSubscriptionAsync(telegramId, code);
        }

        public async Task DeleteSubscription(long telegramId, string code)
        {
            await _database.User.DeleteSubscriptionAsync(telegramId, code);
        }
    }
}
