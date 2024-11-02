using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using Services.Services;
using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using NotifyKP_bot.Interfaces;
using NotifyKP_bot.Services;
using Telegram.Bot;
using Microsoft.Extensions.Logging;


namespace NotifyKP_bot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            //await RunAsync(host.Services);
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
                    services.AddScoped<IUnitOfWork, EntityUnitOfWork>();
                    services.AddDbContext<BotContext>(options =>
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddTransient<IBialaService, BialaService>();
                    services.AddTransient<ITelegramBotService, TelegramBotService>();


                    services.AddSingleton<INotificationService>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<NotificationService>>();
                        var telegramBotService = provider.GetRequiredService<ITelegramBotService>();
                        var eventPublisher = provider.GetRequiredService<IEventPublisher>();
                        var userService = provider.GetRequiredService<IUserService>();

                        var notificationService = new NotificationService(logger, telegramBotService, eventPublisher, userService);
                        eventPublisher.DatesSaved += notificationService.OnDatesSavedAsync;
                        return notificationService;
                    });

                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IBrowserAutomationService, BrowserAutomationService>();

                    services.AddHostedService<ScheduledTaskService>();
                    services.AddScoped<IScheduledTaskService, ScheduledTaskService>();
                });

        private static async Task RunAsync(IServiceProvider services)
        {
            var browserAutomationService = services.GetRequiredService<IBrowserAutomationService>();
            try
            {
                await browserAutomationService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
            }
        }
    }
}
