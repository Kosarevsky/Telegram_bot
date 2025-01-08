namespace Services.Interfaces
{
    public interface IUserActivityChecker
    {
        Task CheckInactiveUsers(DateTime warningThresholdDate, DateTime DeactivationThresholdDate, CancellationToken stoppingToken);
    }
}
