
using Telegram.Bot.Types;

namespace Notifications.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string message);



    }
}
