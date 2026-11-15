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
/// The answers of <see cref="TestAbilityService"/> for the impact lifecycle (part 5). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill)
    {
        Log($"GetAbilityDMG:{creature}:{weapon}:{abilitySkill}");
        return _bench.WeaponDamage;
    }

    protected override CombatDamageType GetWeaponDamageType(uint weapon)
    {
        Log($"GetWeaponDamageType:{weapon}");
        return _bench.WeaponDamageType;
    }

    protected override int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
    {
        Log("ConsumeNextAbilityDamageBonus");
        return _bench.NextAbilityDamageBonus;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(uint creature, SkillType skillType, StatusEffectCategory abilityStatusCategories)
    {
        Log($"ConsumeNextSkillAbilityBonuses:{abilityStatusCategories}");
        return _bench.NextSkillBonuses;
    }

    protected override int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log("GetPersistentCriticalRate");
        return _bench.PersistentCriticalRate;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(uint creature, SkillType skillType)
    {
        Log("ConsumeQueuedWeaponAbilityBonuses");
        return _bench.QueuedBonuses;
    }

    protected override (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature)
    {
        Log("ConsumeGuardedHitBonuses");
        return _bench.GuardedHitBonuses;
    }

    protected override int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability)
    {
        Log($"RefundCritical:{creature}");
        return 0;
    }

    protected override int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log($"ConsumePersistentCriticalRate:{creature}:{skillType}");
        return 0;
    }

    protected override void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) =>
        Log($"NonCriticalEffects:{activator}:{skillType}");
}
