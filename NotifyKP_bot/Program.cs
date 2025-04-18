﻿using Microsoft.Extensions.DependencyInjection;
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
using System.Globalization;

namespace BezKolejki_bot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var culture = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

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
                    services.AddHttpClient();

                    services.AddSingleton<BotContextFactory>();
                    services.AddSingleton<IEventPublisherService, EventPublisherService>();
                    services.AddSingleton<INotificationService>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<NotificationService>>();
                        var telegramBotService = provider.GetRequiredService<ITelegramBotService>();
                        var eventPublisher = provider.GetRequiredService<IEventPublisherService>();
                        var userService = provider.GetRequiredService<IUserService>();
                        var bezKolejkiService = provider.GetRequiredService<IBezKolejkiService>();
                        var notificationService = new NotificationService(logger, telegramBotService, eventPublisher, userService, bezKolejkiService);
                        eventPublisher.DatesSaved += notificationService.OnDatesSavedAsync;
                        return notificationService;
                    });
                    services.AddScoped<IUnitOfWork, EntityUnitOfWork>();
                    services.AddDbContext<BotContext>(options =>
                    {
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                        options.EnableSensitiveDataLogging();
                    }, ServiceLifetime.Transient);

                    services.AddTransient<IBezKolejkiService, BezKolejkiService>();
                    services.AddTransient<BrowserSiteProcessor>();
                    services.AddTransient<OlsztynPostRequestProcessor>();
                    services.AddTransient<GdanskPostRequestProcessor>();
                    services.AddTransient<GdanskQmaticPostRequestProcessor>();
                    services.AddTransient<MoskwaKpPostRequestProcessor>();

                    services.AddTransient<ISiteProcessorFactory, SiteProcessorFactory>();
                    services.AddTransient<IBrowserAutomationService, BrowserAutomationService>();
                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IClientService, ClientService>();
                    services.AddHostedService<ScheduledTaskService>();
                    services.AddHostedService<UserActivityChecker>();
                    services.AddScoped<IUserActivityChecker, UserActivityChecker>();
                    services.AddHostedService<TelegramBotService>();
                    services.AddSingleton<ITelegramBotService, TelegramBotService>();
                    services.AddTransient<ICaptchaRecognitionService, CaptchaRecognitionService>();
                    services.AddTransient<ILocalizationService, LocalizationService>();
                    services.AddTransient<IHttpService, HttpService>();
                    services.AddSingleton<IProxyProvider, RandomProxyProvider>();
                    services.AddHttpClient("ProxyClient")
                        .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(60))
                        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                        {
                            var proxyProvider = serviceProvider.GetRequiredService<IProxyProvider>();

                            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                            bool useProxy = configuration.GetValue("ProxySettings:UseProxy", false);
                            return new HttpClientHandler
                            {
                                Proxy = useProxy ? proxyProvider.GetRandomProxy() : null,
                                UseProxy = useProxy,
                            };
                        });

                    services.AddHttpClient("DefaultClient");

                    services.AddTransient<IHttpService, HttpService>();
                });
    }
}
