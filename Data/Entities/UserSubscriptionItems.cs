using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace Data.Entities
{
    [Table("UserSubscriptionItems")]
    public class UserSubscriptionItems
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [ForeignKey("UserSubscription")]
        public int UserSubscriptionId { get; set; }

        public UserSubscription UserSubscription { get; set; } = new UserSubscription();

        public DateTime AvailableDate { get; set; }

    }
}
