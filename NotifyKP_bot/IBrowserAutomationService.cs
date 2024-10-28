using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyKP_bot
{
    public interface IBrowserAutomationService
    {
        Task<List<DateTime>> GetAvailableDateAsync(string url);
    }
}
