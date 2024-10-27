using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Services.Services;

namespace NotifyKP_bot
{
    public class Program
    {
        private readonly IOperationRecordService _operationRecordService;

        public Program(IOperationRecordService operationRecordService)
        {
            _operationRecordService = operationRecordService;
        }

        static async Task Main()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                });

            builder.ConfigureServices((context, services) =>
            {
                //services.AddAutoMapper(typeof(Program).Assembly);
                services.AddTransient<IOperationRecordService, OperationRecordService>();
                services.AddTransient<IUnitOfWork, EntityUnitOfWork>();

                services.AddDbContext<NotifyKPContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                services.AddTransient<Program>();
            });

            var host = builder.Build();

            var program = host.Services.GetRequiredService<Program>();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            var browserService = new BrowserAutomationService();
            try
            {
                List<DateTime> dates = await browserService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
                _operationRecordService.SaveOperationDate(dates);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
            }
            Console.ReadLine();
        }
    }
}
