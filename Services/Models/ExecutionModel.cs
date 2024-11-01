namespace Services.Models
{
    public class ExecutionModel
    {
        public int Id { get; set; }
        public DateTime ExecutionTime { get; set; }
        public List<AvailableDateModel>? AvailableDates { get; set; }
    }
}
