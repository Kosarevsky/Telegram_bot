namespace Services.Models
{
    public class UserSubscriptionItemsModel
    {
        public int Id { get; set; }

        public int UserSubscriptionId { get; set; }

        public UserSubscriptionModel UserSubscription { get; set; } = new UserSubscriptionModel();

        public DateTime AvailableDate { get; set; }

    }
}
