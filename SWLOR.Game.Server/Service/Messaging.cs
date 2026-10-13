using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service
{
    public class Messaging
    {
        public delegate string BuildMessageDelegate(uint receiver);

        /// <summary>
        /// Sends a message to all nearby players within a certain distance.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message to send to all nearby players.</param>
        /// <param name="range">The range, in meters, to deliver the message. Any creatures outside this range will not see the message.</param>
        public static void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f)
        {
            SendMessageNearbyToPlayers(sender, _ => message, range);
        }

        public static void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range = 10f)
        {
            if (buildMessage == null)
                throw new ArgumentNullException(nameof(buildMessage));

            var profile = FrameProfiler.Start();
            SendMessageToPC(sender, buildMessage(sender));
            var receivers = 1;

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
