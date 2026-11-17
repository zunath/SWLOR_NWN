using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// What the status effect double's seams answer. They are properties of the bench, not of the double, so a seam that several
/// parts of the status effect split call answers the same in each part's double once the parts move.
/// </summary>
public sealed partial class StatusEffectBench
{
    /// <summary>The types the catalog finds; null leaves the catalog empty.</summary>
    public IReadOnlyList<Type>? DefinitionTypes { get; set; }

    public Dictionary<(uint Creature, StatType Stat), int> Stats { get; } = new();

    public AbilityImpactSummary? ActiveImpact { get; set; }

    public Dictionary<FeatType, AbilityDetail> AbilityDetails { get; } = new();

    public HashSet<uint> AttackDelaySuppressed { get; } = new();
}
