using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// What the stat double's seams answer. They are properties of the bench, not of the double, so a seam that several parts of
/// the stat split call answers the same in each part's double once the parts move.
/// </summary>
public sealed partial class StatBench
{
    public Dictionary<(uint Creature, StatType Stat), int> PerkStats { get; } = new();

    public Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> PerkSources { get; } = new();

    public Dictionary<(uint Creature, StatType Stat), int> TemporaryStats { get; } = new();

    public Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> TemporarySources { get; } = new();

    public HashSet<uint> Droids { get; } = new();

    public Dictionary<uint, SkillType> WeaponSkills { get; } = new();

    public Dictionary<SkillType, SkillAttribute> SkillDetails { get; } = new();

    public SkillType BaseItemSkill { get; set; } = SkillType.Invalid;

    public AbilityType WeaponDamageAbility { get; set; } = AbilityType.Might;

    public AbilityType WeaponAccuracyAbility { get; set; } = AbilityType.Perception;

    public int NearbyStatusTargetAdjustment { get; set; }

    public int LowHPAdjustment { get; set; }

    public int LowFPAdjustment { get; set; }

    public bool StatTriggerAvailable { get; set; } = true;

    public HashSet<SkillType> RangedSkills { get; } = new();
}
