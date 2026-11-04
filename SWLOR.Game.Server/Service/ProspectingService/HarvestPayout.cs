using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// A harvest reserved for the player before the node is reduced: the units rolled for <paramref name="Delivery"/>, and the craft file
    /// sequence of the harvest journal entry that holds them with the XP (0 when nothing was journaled).
    /// </summary>
    public sealed record HarvestPayout(HarvestDelivery Delivery, IReadOnlyList<string> Units, int Sequence, int Xp);
}
