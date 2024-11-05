namespace Services.Interfaces
{
    public interface IEventPublisher
    {
        event Func<string, List<DateTime>, Task> DatesSaved;
        Task PublishDatesSavedAsync(string code, List<DateTime> dates);
    }
}
