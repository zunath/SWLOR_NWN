using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>Who a Comms message reaches.</summary>
    public interface ICommunicationAudienceService
    {
        /// <summary>
        /// Splits the online DMs into those who read a Comms message and those whose Comms filter hides it.
        /// Each DM is identified by the creature they control, since chat sent to a possessing DM's avatar is dropped.
        /// </summary>
        /// <param name="sender">The creature that sent the Comms message.</param>
        /// <param name="onlinePlayers">Every object returned by GetFirstPC/GetNextPC.</param>
        (List<uint> Readers, HashSet<uint> Muted) GetDMCommsAudience(uint sender, List<uint> onlinePlayers);

        /// <summary>True when a Comms message from <paramref name="sender"/> reaches <paramref name="receiver"/> by the sender's current location scope.</summary>
        bool IsCommsReceiverInRange(uint sender, uint receiver);
    }
}
