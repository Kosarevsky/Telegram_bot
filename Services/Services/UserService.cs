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
                cfg.CreateMap<Data.Entities.User, UserModel>()
                    .ForMember(dest => dest.Subscriptions, opt => opt.MapFrom(src => src.Subscriptions))
                    .ReverseMap();

                cfg.CreateMap<UserSubscription, UserSubscriptionModel>()
                    .ForMember(dest => dest.UserSubscriptionItems, opt => opt.MapFrom(src => src.UserSubscriptionItems))
                    .ReverseMap();

                cfg.CreateMap<UserSubscriptionItems, UserSubscriptionItemsModel>().ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        public async Task<List<UserModel>> GetAllAsync(Expression<Func<Data.Entities.User, bool>>? predicate = null)
        {
            var users = predicate == null
                ? await _database.User.GetAllAsync() 
                : await _database.User.GetAllAsync(predicate);

            return _mapper.Map<List<UserModel>>(users);
        }

        public async Task SaveSubscription(long telegramId, string code, List<DateTime>? dates = null)
        {
           await _database.User.SaveSubscriptionAsync(telegramId, code, dates);
        }

        public async Task DeleteSubscription(long telegramId, string code)
        {
            await _database.User.DeleteSubscriptionAsync(telegramId, code);
        }

    }
}
