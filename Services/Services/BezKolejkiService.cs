using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class BezKolejkiService : IBezKolejkiService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;
        private readonly ILogger<BezKolejkiService> _logger;
        private readonly IEventPublisherService _eventPublisherService;


        public BezKolejkiService(IUnitOfWork database, ILogger<BezKolejkiService> logger, IEventPublisherService eventPublisherService)
        {
            _database = database;
            _logger = logger;
            _eventPublisherService = eventPublisherService;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Data.Entities.User, UserModel>()
                    .ForMember(dest => dest.Subscriptions, opt => opt.MapFrom(src => src.Subscriptions))
                    .ReverseMap();

                cfg.CreateMap<UserSubscription, UserSubscriptionModel>()
                    .ReverseMap();
            });

            _mapper = config.CreateMapper();
            _eventPublisherService = eventPublisherService;
        }

        public async Task SaveAsync(string code, List<DateTime> dates)
        {
            var ex = new Execution
            {
                Code = code,
                AvailableDates = dates.Select(d=> new AvailableDate { Date = d}).ToList(),
            };
            await _database.Executions.SaveOperationWithDatesAsync(ex);
        }

        public async Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code) 
        {
            var executions = await _database.Executions
                .GetAll(d => d.Code == code)
                .OrderByDescending(d => d.ExecutionDateTime)
                .FirstOrDefaultAsync();
            var dates = executions?.AvailableDates.Select(d => d.Date).ToList();
            return dates ?? new List<DateTime>();
        }

        public async Task<List<UserModel>> GetActiveUsers()
        {
            var activeUsers = await _database.User.GetAllAsync(u => u.IsActive && u.Subscriptions.Any()).ToListAsync();
            return _mapper.Map<List<UserModel>>(activeUsers);
        }

        public string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;

            return text.Length > maxLength
                ? text.Substring(0, maxLength - 3) + "..."
                : text;
        }

        public async Task SaveDatesToDatabase(List<DateTime> dates, List<DateTime> previousDates, string code)
        {
            var buttonName = TruncateText(CodeMapping.GetKeyByCode(code), 60);
            if (dates != null && (dates.Any() || previousDates.Any()))
            {
                if (!string.IsNullOrEmpty(code))
                {
                    try
                    {
                        await SaveAsync(code, dates);
                        _logger.LogInformation($"Save date to {code}, {buttonName}");
                        await _eventPublisherService.PublishDatesSavedAsync(code, dates, previousDates);
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning($"Error save or published data {code}");
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning($"No code found for button ({code} {buttonName})");
                }
            }
            else
            {
                _logger.LogInformation($"No dates available to save ({code} {buttonName})");
            }
        }

        public async Task<bool> ProcessingDate(bool dataSaved, List<DateTime> dates, string code)
        {
            _logger.LogInformation($"Processing date for {code}");
            _logger.LogInformation($"Data: {string.Join(", ", dates)}");
            var previousDates = new List<DateTime>();
            try
            {
                previousDates = await GetLastExecutionDatesByCodeAsync(code);
            }
            catch (Exception)
            {
                _logger.LogWarning($"Error loading previousDates {code}");
            }

            if ((dates.Any() || previousDates.Any()) && !dataSaved)
            {
                await SaveDatesToDatabase(dates, previousDates, code);
                dataSaved = true;

            }
            else if (!dates.Any())
            {
                _logger.LogInformation($"{code}. Not available date for save");
            }

            return dataSaved;
        }

        public async Task<int> GetCountActiveUsersByCode(string code)
        {
            var activeUsers = await GetActiveUsers();
            var countByActiveUsers = activeUsers
                .Where(u => u.Subscriptions.Any(s => s.SubscriptionCode == code))
                .Count();

            return countByActiveUsers;
        }
    }
}
