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
        public string TelegramNickName { get; set; } = string.Empty;

        [Required]
        public DateTime DateLastSubscription {  get; set; }

        public Boolean IsActive { get; set; } = true;

        public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    }
}
