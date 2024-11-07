namespace Services.Models
{
    public class UserSubscriptionModel
    {
        public int Id { get; set; }

        public long UserId { get; set; }
        public UserModel User { get; set; } = null!;

        public string SubscriptionCode { get; set; } = null!;
        public DateTime SubscriptionDateTime { get; set; }
    }
}
