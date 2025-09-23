using System.Drawing;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IDiscordNotificationService
    {
        void PublishMessage(string author, string message, Color color, DiscordNotificationType type);
    }
}
