using SWLOR.Component.Communication.Contracts;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.Shared.Domain.Communication.Contracts;

namespace SWLOR.Component.Communication.Service
{
    public class Messaging : IMessagingService
    {
        /// <summary>
        /// Sends a message to all nearby players within a certain distance.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message to send to all nearby players.</param>
        /// <param name="range">The range, in meters, to deliver the message. Any creatures outside this range will not see the message.</param>
        public void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f)
        {
            SendMessageToPC(sender, message);

            int nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(sender, nearby) <= range)
            {
                if (sender == nearby) continue;

                SendMessageToPC(nearby, message);
                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            }
        }
    }
}
