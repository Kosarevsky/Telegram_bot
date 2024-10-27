using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("OperationRecord")]
    public class OperationRecord
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public DateTime ExecutionTime { get; set; }
 
        public List<DateRecord> DateRecords { get; set; } = new List<DateRecord>();
    }
}
