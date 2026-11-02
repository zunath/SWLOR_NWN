using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>A traveler that is due to move, and where it has been.</summary>
    /// <param name="Type">The traveler.</param>
    /// <param name="History">The weeks it was hosted, in any order.</param>
    public readonly record struct TravelerRotationInput(TravelerType Type, IReadOnlyList<TravelerVisit> History);
}
