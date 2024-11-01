using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("UserSubscription")]
    public class UserSubscription
    {
        [Key]
        [Required]
        public int Id { get; set; }
        
        [Required]
        [ForeignKey("User")]
        public long UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string SubscriptionCode { get; set; } = null!;
    }
}
