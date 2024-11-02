using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Services.Interfaces;
using Services.Models;
using System.Linq.Expressions;
using Telegram.Bot.Types;


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
                    .ReverseMap();

                cfg.CreateMap<UserSubscription, UserSubscriptionModel>().ReverseMap();
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

/*        public Task<List<UserModel>> GetAsync()
        {
            var users = _database.User.GetAsync();
            return Task.FromResult(_mapper.Map<List<UserModel>>(users));
        }*/


/*        public async Task<UserModel?> GetByTelegramIdAsync(long telegramId)
        {
            var users = await _database.User.GetAsync();
            var user = users.FirstOrDefault(u => u.TelegramUserId == telegramId);
            return _mapper.Map<UserModel>(user);
        }*/

        public async Task SaveSubscription(long telegramId, string code)
        {
           await _database.User.SaveSubscriptionAsync(telegramId, code);
        }


        public async Task DeleteSubsription(long telegramId, string code)
        {
            await _database.User.DeleteSubscriptionAsync(telegramId, code);
        }

    }
}
