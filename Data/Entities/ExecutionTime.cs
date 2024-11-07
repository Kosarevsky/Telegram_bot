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

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public ICollection<AvailableDate> AvailableDates { get; set; } = new List<AvailableDate>();
    }
}
