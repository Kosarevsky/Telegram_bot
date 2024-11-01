using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("Execution")]
    public class Execution
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public DateTime ExecutionDateTime { get; set; }
 
        public ICollection<AvailableDate> AvailableDates { get; set; } = new List<AvailableDate>();
    }
}
