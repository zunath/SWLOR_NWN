using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Messaging: the logic that used to be static in <c>Service.Messaging</c>, moved here unchanged. The facade forwards
    /// every call to the container's instance.
    /// </summary>
    public class MessagingService : IMessagingService
    {
        public void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f)
        {
            SendMessageNearbyToPlayers(sender, _ => message, range);
        }

        public void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range = 10f)
        {
            if (buildMessage == null)
                throw new ArgumentNullException(nameof(buildMessage));

            var profile = FrameProfiler.Start();
            SendMessageToPC(sender, buildMessage(sender));
            var receivers = profile != 0 && GetIsPC(sender) ? 1 : 0;

            int nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(sender, nearby) <= range)
            {
                if (sender != nearby)
                {
                    SendMessageToPC(nearby, buildMessage(nearby));
                    receivers++;
                }

                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            }

            FrameProfiler.Stop(FrameMetric.Broadcast, profile);
            FrameProfiler.Count(FrameMetric.BroadcastReceiver, receivers);
        }
    }
}
