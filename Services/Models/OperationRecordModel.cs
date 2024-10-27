
namespace Services.Models
{
    public class OperationRecordModel
    {
        public int Id { get; set; }
        public DateTime ExecutionTime { get; set; }
        public List<DateRecordModel>? DateRecords { get; set; }
    }
}
