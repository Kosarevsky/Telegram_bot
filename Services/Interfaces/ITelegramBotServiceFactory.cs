using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Services.Interfaces
{
    public interface ITelegramBotServiceFactory
    {
        ITelegramBotService Create();
    }

    public class TelegramBotServiceFactory : ITelegramBotServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TelegramBotServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITelegramBotService Create()
        {
            var botClient = _serviceProvider.GetRequiredService<ITelegramBotClient>();
            var logger = _serviceProvider.GetRequiredService<ILogger<TelegramBotService>>();
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var userService = _serviceProvider.GetRequiredService<IUserService>();
            var notificationService = _serviceProvider.GetRequiredService<INotificationService>();


            return new TelegramBotService(botClient, logger, configuration, userService);
        }
    }
}
