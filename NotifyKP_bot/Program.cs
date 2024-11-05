using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using Services.Services;
using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Services;
using Telegram.Bot;
using Microsoft.Extensions.Logging;


namespace BezKolejki_bot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Host has been built successfully.");

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((context, services) =>
                {
                    var botToken = context.Configuration["Telegram:BotToken"];
                    if (string.IsNullOrEmpty(botToken))
                    {
                        throw new InvalidOperationException("Bot token is not configured.");
                    }

                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

                    services.AddSingleton<IEventPublisher, EventPublisher>();
                    services.AddSingleton<INotificationService>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<NotificationService>>();
                        var telegramBotService = provider.GetRequiredService<ITelegramBotService>();
                        var eventPublisher = provider.GetRequiredService<IEventPublisher>();
                        var userService = provider.GetRequiredService<IUserService>();

                        var notificationService = new NotificationService(logger, telegramBotService, eventPublisher, userService);
                        eventPublisher.DatesSaved += notificationService.OnDatesSavedAsync;
                        logger.LogWarning("*** NotificationService registered.");
                        return notificationService;
                    });
                    services.AddScoped<IUnitOfWork, EntityUnitOfWork>();
                    services.AddDbContext<BotContext>(options =>
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddTransient<IBialaService, BialaService>();
     
                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IBrowserAutomationService, BrowserAutomationService>();

                    services.AddHostedService<ScheduledTaskService>();
                    services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();

                    services.AddHostedService<TelegramBotService>();
                    services.AddSingleton<ITelegramBotService, TelegramBotService>();
                });
    }
}
