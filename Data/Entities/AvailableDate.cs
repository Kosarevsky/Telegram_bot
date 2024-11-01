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
        public int OperationId { get; set; }
        public Execution Execution { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

    }
}
