using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Messaging: the logic that used to be static in the <c>Messaging</c> facade, moved here unchanged. Static callers
    /// reach the container's instance through <c>GameServices.Messaging</c>.
    /// </summary>
    public class MessagingService : IMessagingService
    {
        private readonly IStealthService _stealth;

        public MessagingService(IStealthService stealth)
        {
            _stealth = stealth;
        }

        // Whether a creature is in stealth or invisible. Reads the creature's effects through the native library, so a unit
        // test subclass answers this instead.
        protected virtual bool IsConcealed(uint creature) => _stealth.IsConcealed(creature);

        public bool ShouldDeliverNearby(bool senderConcealed, bool actorConcealed)
        {
            return !senderConcealed && !actorConcealed;
        }

        /// <summary>
        /// Floats text over a creature for every player within range. A creature in stealth or invisible only shows it to itself.
        /// </summary>
        public void FloatTextNearbyToPlayers(uint creature, string text, float range = 30f)
        {
            if (GetIsPC(creature))
                PlayerPlugin.FloatingTextStringOnCreature(creature, creature, text, false);

            if (IsConcealed(creature))
                return;

            var nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, creature, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(creature, nearby) <= range)
            {
                PlayerPlugin.FloatingTextStringOnCreature(nearby, creature, text, false);
                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, creature, nth);
            }
        }

        public void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f)
        {
            SendMessageNearbyToPlayers(sender, _ => message, range);
        }

        public void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range = 10f)
        {
            SendMessageNearbyToPlayers(sender, buildMessage, range, OBJECT_INVALID);
        }

        public void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range, uint concealedActor)
        {
            if (buildMessage == null)
                throw new ArgumentNullException(nameof(buildMessage));

            if (!ShouldDeliverNearby(IsConcealed(sender), IsConcealed(concealedActor)))
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
