using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    [Table("User")]
    public class User
    {
        [Key]
        [Required]
        public long Id { get; set; }

        [Required]
        public long TelegramUserId { get; set; }

        public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    }
}
