using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The properties an enhancement transfers to the recipe's item and the progress it adds to the craft.</summary>
public sealed class CraftEnhancementReading
{
    public List<ItemProperty> Properties { get; } = new();

    /// <summary>Extra progress the enhancement adds to the progress meter's maximum.</summary>
    public int ProgressPenalty { get; set; }
}
