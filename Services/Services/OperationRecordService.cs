using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Notifications.Interfaces;
using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class OperationRecordService : IOperationRecordService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;
        public OperationRecordService(IUnitOfWork database, INotificationService notificationService)
        {
            _database = database;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<OperationRecord, OperationRecordModel>()
                    .ForMember(d => d.DateRecords, s => s.MapFrom(x => x.DateRecords))
                    .ReverseMap()
                    .ForMember(d => d.ExecutionTime, s => s.MapFrom(x => x.ExecutionTime));

                cfg.CreateMap<DateRecord, DateRecordModel>().ReverseMap(); 
            });

            _mapper = config.CreateMapper();
        }
        public async void SaveOperationDate(ICollection<DateTime> dates)
        {
            var op = new OperationRecordModel
            {
                ExecutionTime = await _database.GetCurrentDateTimeFromSQLServer(),
                DateRecords = new List<DateRecordModel>() 
            };

            foreach (var date in dates)
            {
                op.DateRecords.Add(new DateRecordModel
                {
                    Date = date,
                });
            }

            var ef = _mapper.Map<OperationRecord>(op);

            await _database.Operations.SaveOperationWithDatesAsync(_mapper.Map<OperationRecord>(op));
        }
    }
}
