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
/// The answers of <see cref="TestAbilityService"/> for the hostile impact (part 12). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("ConsumeStatusAppliedBonus");
        return _bench.StatusAppliedBonus;
    }

    protected override bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
    {
        Log("IsTargetAttackingAlly");
        return _bench.AttackingAlly;
    }

    protected override void ApplySuccessfulAbilityImpactRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType, CombatDamageType damageType, int damage, bool statusApplied, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, bool firstHostileAbilityHitDamageBonusApplied, bool isFirstSuccessfulTarget, StatusEffectCategory appliedStatusCategories, bool allowsHitProcs, bool targetWasAttackingAlly) =>
        Log($"ImpactRiders:{target}:{damage}:{statusApplied}:{firstHostileAbilityHitDamageBonusApplied}:{isFirstSuccessfulTarget}:{appliedStatusCategories}:{allowsHitProcs}:{targetWasAttackingAlly}");

    protected override bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon) => _bench.IncludesWeaponDamage;
}
