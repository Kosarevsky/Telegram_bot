namespace Services.Models
{
    public class ExecutionModel 
    {
        public int Id { get; set; }
        public DateTime ExecutionDateTime { get; set; }

        public string Code { get; set; } = string.Empty;

        public List<AvailableDateModel> AvailableDates { get; set; } = new List<AvailableDateModel>();
    }
}
