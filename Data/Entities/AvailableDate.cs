using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("AvailableDate")]
    public class AvailableDate
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [ForeignKey("Execution")]
        public int ExecutionId { get; set; }
        public Execution Execution { get; set; } = null!;
    }
}
