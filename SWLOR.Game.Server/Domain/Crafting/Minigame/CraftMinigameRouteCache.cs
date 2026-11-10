using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame;

/// <summary>The minigame routes found by <see cref="SWLOR.Game.Server.Domain.Crafting.CraftMinigameRoutingService"/>, filled on first use. <c>CraftMinigameRoutingService</c> owns the one cache.</summary>
public sealed class CraftMinigameRouteCache
{
    /// <summary>The route of each crafting skill, or null before the first lookup.</summary>
    public IReadOnlyDictionary<SkillType, CraftMinigameRoute> Routes { get; set; }
}
