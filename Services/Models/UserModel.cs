namespace Services.Models
{
    public class UserModel
    {
        public long Id { get; set; }

        public long TelegramUserId { get; set; }

        public DateTime DateLastSubscription { get; set; }
        public Boolean IsActive { get; set; }

        public List<UserSubscriptionModel> Subscriptions { get; set; } = new List<UserSubscriptionModel>();

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Title { get; set; }
    }
}
