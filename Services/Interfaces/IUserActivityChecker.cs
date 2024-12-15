using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IUserActivityChecker
    {
        Task CheckInactiveUsers(DateTime warningThresholdDate, DateTime DeactivationThresholdDate, CancellationToken stoppingToken);
    }
}
