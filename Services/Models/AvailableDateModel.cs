namespace Services.Models
{
    public class AvailableDateModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int OperationId { get; set; }
        public ExecutionModel Execution { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
    }
}
