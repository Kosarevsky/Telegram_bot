using System.ComponentModel.DataAnnotations;

namespace Data
{
    public class OperationRecord
    {
        [Key]
        public int Id { get; set; }
        public DateTime ExecutionTime { get; set; }
        public ICollection<DateRecord>? DateRecords { get; set; }
    }
}
