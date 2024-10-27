using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data
{
    public class DateRecord
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public int OperationId { get; set; }
        [ForeignKey("OperationId")]
        [Required]
        public OperationRecord Operation { get; set; }

    }
}
