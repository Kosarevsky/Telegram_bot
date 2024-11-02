using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotifyKP_bot.Interfaces;
using NotifyKP_bot.Services;
using Services.Interfaces;
using Services.Services;
using Telegram.Bot;

namespace NotifyKP_bot
{
    public class Program
    {
        private readonly IBrowserAutomationService _browserAutomationService;

        public Program(IBrowserAutomationService browserAutomationService)
        {
            _browserAutomationService = browserAutomationService;
        }

        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.json");
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile(path, optional: false, reloadOnChange: true);
                })
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
                    services.AddScoped<INotificationService, NotificationService>();
                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IBrowserAutomationService, BrowserAutomationService>();

                    services.AddHostedService<ScheduledTaskService>();
                    services.AddScoped<IScheduledTaskService, ScheduledTaskService>();

                    //services.AddHostedService<TelegramBotService>();
                })
                .Build();

            await host.RunAsync(); 
        }


        public async Task RunAsync()
        {
            try
            {
                await _browserAutomationService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
            }
        }
    }
}
