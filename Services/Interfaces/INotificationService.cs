namespace Services.Interfaces
{
    public interface INotificationService
    {
        Task NotificationSend(string code, List<DateTime> dates, List<DateTime> sendedDates);
    }
}
