namespace Services.Models
{
    public class UserModel
    {
        public long Id { get; set; }

        public long TelegramUserId { get; set; }

        public string TelegramNickName { get; set; } = string.Empty;

        public DateTime DateLastSubscription { get; set; }
        public Boolean IsActive { get; set; }

        public List<UserSubscriptionModel> Subscriptions { get; set; } = new List<UserSubscriptionModel>();

    }
}
