using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
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
                    .ReverseMap();
            });

            _mapper = config.CreateMapper();
        }

        public async Task<List<UserModel>> GetAllAsync(Expression<Func<Data.Entities.User, bool>>? predicate = null)
        {
            var users = predicate == null
                ? await _database.User.GetAllAsync().ToListAsync() 
                : await _database.User.GetAllAsync(predicate).ToListAsync();

            return _mapper.Map<List<UserModel>>(users);
        }

        public async Task<UserModel> GetByTelegramId(long telegramId)
        {
            var user = await _database.User.GetAllAsync(u => u.TelegramUserId == telegramId).FirstOrDefaultAsync();
            return _mapper.Map<UserModel>(user);
        }
        public async Task SaveSubscription(long telegramId, string code)
        {
           await _database.User.SaveSubscriptionAsync(telegramId, code);
        }

        public async Task DeleteSubscription(long telegramId, string code)
        {
            await _database.User.DeleteSubscriptionAsync(telegramId, code);
        }

        public async Task UpdateLastNotificationDateAsync(UserModel tgUser)
        {
            var userEntity = _mapper.Map<Data.Entities.User>(tgUser);
            await _database.User.UpdateLastNotificationDateAsync(userEntity);
        }

        public void DeactivateUserAsync(long chatId)
        {
            var user = _database.User.GetAllAsync(u => u.TelegramUserId == chatId).FirstOrDefault();
            if (user != null) { 
            user.IsActive = false;
                _database.User.DeactivateUserAsync(user);
            }

        }
    }
}
