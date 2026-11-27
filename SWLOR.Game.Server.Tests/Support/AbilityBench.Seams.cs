using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The answers of the calls into unconverted code (the <c>protected virtual</c> seams of the ability parts), which every part
/// double reads, and what those calls recorded (<see cref="StatSourceReads"/>, <see cref="CachedTargeting"/>).
/// </summary>
public sealed partial class AbilityBench
{
    public IReadOnlyList<Type>? DefinitionTypes { get; set; }
    public Dictionary<(uint Creature, StatType Stat), int> Stats { get; } = new();
    public IReadOnlyList<StatAdjustmentSource> StatSources { get; set; } = Array.Empty<StatAdjustmentSource>();
    public int StatSourceReads { get; set; }
    public HashSet<uint> SpaceMode { get; } = new();
    public string EquipmentError { get; set; } = string.Empty;
    public int NextAbilityDamageBonus { get; set; }
    public (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) NextSkillBonuses { get; set; }
    public int PersistentCriticalRate { get; set; }
    public (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) QueuedBonuses { get; set; }
    public (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) GuardedHitBonuses { get; set; }
    public int StatusAppliedBonus { get; set; }
    public int WeaponDamage { get; set; }
    public CombatDamageType WeaponDamageType { get; set; } = CombatDamageType.Physical;
    public HashSet<SkillType> WeaponSkills { get; } = new() { SkillType.Vibroblade, SkillType.HeavyVibroblade, SkillType.Pistol, SkillType.Rifle };
    public bool HasEquippedWeapon { get; set; } = true;
    public bool IncludesWeaponDamage { get; set; }
    public bool RepliesActiveFeat { get; set; } = true;
    public bool EnforcesFeatReplacement { get; set; }
    public bool HitResolves { get; set; } = true;
    public int HitRate { get; set; } = 75;
    public int CriticalRating { get; set; }
    public bool CriticalDowngraded { get; set; }
    public int IncomingDamageTakenPercent { get; set; } = 100;
    public int FlatBaseDamageBonus { get; set; }
    public (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) IdleBonuses { get; set; }
    public int AttackValue { get; set; } = 40;
    public int DefenseValue { get; set; } = 20;
    public NPCStats NpcStats { get; set; } = new();
    public int CurrentFP { get; set; } = 100;
    public int MaxFP { get; set; } = 100;
    public int CurrentStamina { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    public int CombatReadiness { get; set; }
    public float ForceAffinityMultiplier { get; set; } = 1f;
    public float ForceAffinityRestorativeMultiplier { get; set; } = 1f;
    public int StatusDurationAdjustment { get; set; }
    public bool AttackingAlly { get; set; }
    public IReadOnlyDictionary<FeatType, AbilityDetail>? CachedTargeting { get; set; }

    /// <summary>
    /// The damage-derived healing scope the per-target resolution and the hostile impact open: logged when it opens and when it
    /// closes, so a test can see what ran inside it.
    /// </summary>
    public IDisposable BeginHealingScope(uint creature, uint target)
    {
        Log.Add($"HealingScope:{creature}:{target}");
        return new HealingScope(this);
    }

    private sealed class HealingScope : IDisposable
    {
        private readonly AbilityBench _bench;

        public HealingScope(AbilityBench bench)
        {
            _bench = bench;
        }

        public void Dispose() => _bench.Log.Add("HealingScopeEnd");
    }
}
