using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("DateRecord")]
    public class DateRecord
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [ForeignKey("Operation")]
        public int OperationId { get; set; }
        public OperationRecord Operation { get; set; }

    }
}
