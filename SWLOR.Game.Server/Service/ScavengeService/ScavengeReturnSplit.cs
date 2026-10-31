using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>What a return of Search finds put in the player's inventory and what had no room.</summary>
    /// <param name="Delivered">The quantities now in the inventory, by resref.</param>
    /// <param name="Remainder">The quantities that did not fit, by resref. They must stay on the player's record.</param>
    public sealed record ScavengeReturnSplit(
        IReadOnlyDictionary<string, int> Delivered,
        IReadOnlyDictionary<string, int> Remainder);
}
