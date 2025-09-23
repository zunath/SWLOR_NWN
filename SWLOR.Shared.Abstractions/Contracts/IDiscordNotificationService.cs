using System.Drawing;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IDiscordNotificationService
    {
        void PublishMessage(
            string author, 
            string message, 
            Color color, 
            DiscordNotificationType type,
            string title = null,
            List<DiscordNotificationField> fields = null);
    }
}
