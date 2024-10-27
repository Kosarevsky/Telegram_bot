namespace Data.Interfaces
{
    public interface IOperationRepository
    {
        Task SaveOperationWithDatesAsync(IEnumerable<DateTime> dates);

    }
}
