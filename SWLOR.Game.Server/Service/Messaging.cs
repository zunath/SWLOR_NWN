using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class Messaging
    {
        /// <summary>
        /// Sends a message to all nearby players within 10 meters.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message to send to all nearby players.</param>
        public static void SendMessageNearbyToPlayers(uint sender, string message)
        {
            const float MaxDistance = 10.0f;

            SendMessageToPC(sender, message);

            int nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(sender, nearby) <= MaxDistance)
            {
                if (sender == nearby) continue;

                SendMessageToPC(nearby, message);
                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, sender, nth);
            }
        }
    }
}
