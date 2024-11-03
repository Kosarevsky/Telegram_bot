namespace Services.Interfaces
{
    public interface IBialaService
    {
        Task SaveAsync(List<DateTime> dates, string code);
    }
}
