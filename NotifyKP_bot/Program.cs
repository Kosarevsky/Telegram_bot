using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Interfaces;
using Notifications.Services;
using Services.Interfaces;
using Services.Services;
using Telegram.Bot;

namespace NotifyKP_bot
{
    public class Program
    {
        private readonly IOperationRecordService _operationRecordService;
        private readonly IBrowserAutomationService _browserAutomationService;

        public Program(IOperationRecordService operationRecordService, IBrowserAutomationService browserAutomationService)
        {
            _operationRecordService = operationRecordService;
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
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    var botToken = context.Configuration["Telegram:BotToken"];
                    if (string.IsNullOrEmpty(botToken))
                    {
                        throw new InvalidOperationException("Bot token is not configured.");
                    }

                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddHostedService<TelegramBotService>();
                    services.AddTransient<INotificationService, TelegramBotService>();

                    services.AddTransient<IOperationRecordService, OperationRecordService>();
                    services.AddTransient<IBrowserAutomationService, BrowserAutomationService>();
                    services.AddTransient<IUnitOfWork, EntityUnitOfWork>();

                    services.AddDbContext<NotifyKPContext>(options =>
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddTransient<Program>();
                })
                .Build();

            await host.RunAsync();  // Завершение Main после завершения приложения
        }


        public async Task RunAsync()
        {
            try
            {
                // Выполнение основной задачи
                var dates = await _browserAutomationService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
                _operationRecordService.SaveOperationDate(dates);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
            }
        }
    }
}
