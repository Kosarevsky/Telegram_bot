
namespace Services.Interfaces
{
    public interface IOperationRecordService
    {
        void SaveOperationDate(ICollection<DateTime> dates);
    }
}
