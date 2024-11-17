namespace Services.Interfaces
{
    public interface IEventPublisherService
    {
        event Func<string, List<DateTime>, List<DateTime>, long?, Task> DatesSaved;
        Task PublishDatesSavedAsync(string code, List<DateTime> dates, List<DateTime> sendedDates, long? telegramId = null);
    }
}
