namespace Services.Models
{
    public class DateRecordModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int OperationId { get; set; }
        public OperationRecordModel Operation { get; set; }
    }
}
