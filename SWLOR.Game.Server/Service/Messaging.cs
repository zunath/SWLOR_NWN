using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service
{
    public class Messaging
    {
        public delegate string BuildMessageDelegate(uint receiver);

        public static bool ShouldDeliverNearby(bool senderConcealed, bool actorConcealed)
        {
            return !senderConcealed && !actorConcealed;
        }

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
            SendMessageNearbyToPlayers(sender, buildMessage, range, OBJECT_INVALID);
        }

        /// <summary>
        /// Sends a message to the sender and every player within range. While the sender, or the
        /// <paramref name="concealedActor"/> the message names, is in stealth or invisible, the message
        /// goes only to the sender and that actor instead, so it never tells nearby players a hidden
        /// creature is there.
        /// </summary>
        public static void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range, uint concealedActor)
        {
            if (buildMessage == null)
                throw new ArgumentNullException(nameof(buildMessage));

            if (!ShouldDeliverNearby(Stealth.IsConcealed(sender), Stealth.IsConcealed(concealedActor)))
            {
                SendMessageToPC(sender, buildMessage(sender));
                if (GetIsObjectValid(concealedActor) && concealedActor != sender && GetIsPC(concealedActor))
                    SendMessageToPC(concealedActor, buildMessage(concealedActor));
                return;
            }

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
