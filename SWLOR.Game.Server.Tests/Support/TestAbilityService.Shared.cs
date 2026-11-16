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
/// The answers of <see cref="TestAbilityService"/> for the calls into unconverted code that several parts of the ability
/// split make (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>): each part that moves takes a copy of the overrides it
/// calls. The answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    // Called by Dark Force and the damage formulas; the aura and activation parts have their own (TestAbilityAuraService,
    // TestAbilityActivationService).
    protected override int GetStatAdjustment(uint creature, StatType stat) => _bench.Stats.GetValueOrDefault((creature, stat));

    // Called by the damage formulas; the magnitude part has its own (TestCombatAbilityMagnitudeService).
    protected override int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => amount + 2;

    // Called by the impact lifecycle; the activation part's Force power check answers with the same skill.
    protected override SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => ability.SkillType;

    // Called by the impact lifecycle and the telegraphed impact.
    protected override int GetStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("GetStatusAppliedBonus");
        return _bench.StatusAppliedBonus;
    }

    // Called by the impact lifecycle and the damage formulas.
    protected override int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
        triggeringWeaponDamage ?? _bench.WeaponDamage;

    // Called by the impact lifecycle and the hostile impact.
    protected override CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) => _bench.WeaponDamageType;

    // Called by the damage formulas and the weapon trait reads; the activation part has its own (TestAbilityActivationService).
    protected override bool IsWeaponSkillType(SkillType skillType) => _bench.WeaponSkills.Contains(skillType);

    // Called by the impact lifecycle and the telegraphed impact.
    protected override void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) =>
        Log($"ApplyAbilityImpactEffects:{activator}:{summary.ImpactedTargetCount}");

    // Called by the per-target resolution and the hostile impact.
    protected override IDisposable BeginDamageDerivedHealing(uint creature, uint target)
    {
        Log($"HealingScope:{creature}:{target}");
        return new HealingScope(_bench);
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
