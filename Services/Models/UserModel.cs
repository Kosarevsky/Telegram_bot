namespace Services.Models
{
    public class UserModel
    {
        public long Id { get; set; }

        public long TelegramUserId { get; set; }

        public List<UserSubscriptionModel> Subscriptions { get; set; } = new List<UserSubscriptionModel>();
    }
}
