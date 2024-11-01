using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Notifications.Interfaces;
using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class BialaService : IBialaService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public BialaService(IUnitOfWork database, INotificationService notificationService)
        {
            _database = database;
            _notificationService = notificationService;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Execution, ExecutionModel>()
                    .ForMember(d => d.ExecutionTime, s => s.MapFrom(x => x.ExecutionDateTime))
                    .ReverseMap();


                cfg.CreateMap<AvailableDate, AvailableDateModel>().ReverseMap(); 
            });

            _mapper = config.CreateMapper();
        }
        public async void Save(List<DateTime> dates, string code)
        {
            var op = new ExecutionModel
            {
                ExecutionTime = await _database.GetCurrentDateTimeFromSQLServer(),
                AvailableDates = new List<AvailableDateModel>(),
            };

            foreach (var date in dates)
            {
                op.AvailableDates.Add(new AvailableDateModel
                {
                    Date = date,
                    Code = code
                });
            }

            //var ef = _mapper.Map<Execution>(op);

            await _database.Executions.SaveOperationWithDatesAsync(_mapper.Map<Execution>(op));
            //await _notificationService.SendNotificationAsync("Найдены новые даты: " + string.Join(", ", dates));
        }
    }
}
