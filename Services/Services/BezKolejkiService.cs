using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Models;
using System.Linq.Expressions;

namespace Services.Services
{
    public class BezKolejkiService : IBezKolejkiService
    {
        private readonly IUnitOfWork _database;
        private readonly IMapper _mapper;

        public BezKolejkiService(IUnitOfWork database)
        {
            _database = database;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Execution, ExecutionModel>()
                    .ReverseMap();
                cfg.CreateMap<AvailableDate, AvailableDateModel>().ReverseMap(); 
            });

            _mapper = config.CreateMapper();
        }

        public async Task SaveAsync(string code, List<DateTime> dates)
        {
            var ex = new ExecutionModel
            {
                Code = code,
                AvailableDates = dates.Select(d=> new AvailableDateModel { Date = d}).ToList(),
            };
            await _database.Executions.SaveOperationWithDatesAsync(_mapper.Map<Execution>(ex));
        }

        public async Task<List<ExecutionModel>> GetAllExecutionAsync(Expression<Func<Execution, bool>>? predicate = null)
        {
            var dates = predicate == null
                ? _database.Executions.GetAll()
                : _database.Executions.GetAll(predicate);
            var result = await dates.ToListAsync();
            return _mapper.Map<List<ExecutionModel>>(result);
        }

        public async Task<List<DateTime>> GetLastExecutionDatesByCodeAsync(string code) 
        {
            var executions = await _database.Executions
                .GetAll()
                .Where(d => d.Code == code)
                .OrderByDescending(d => d.ExecutionDateTime)
                .FirstOrDefaultAsync();
            var dates = executions?.AvailableDates.Select(d => d.Date).ToList();
            return dates ?? new List<DateTime>();
        }
    }
}
