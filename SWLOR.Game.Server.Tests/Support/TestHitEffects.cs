#nullable enable
using FluentAssertions;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using System.Collections;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class TestHitEffects : CombatHitEffectsService
{
    private readonly List<string> _log;
    private readonly Func<Func<int, int>> _taken;

    public TestHitEffects(
        List<string> log,
        Func<Func<int, int>> taken,
        ICombatQueriesService queries,
        ICombatResourcesService resources,
        ICombatAttackTimingService attackTiming,
        ICombatSuppressionService suppression,
        ICombatActivityService combatActivity,
        ICombatDamageModifiersService damageModifiers,
        ICombatRepeatedTargetService repeatedTarget,
        ICombatAbilityHitService abilityHit,
        IStatService stat,
        IStatusEffectService statusEffect,
        IStatusEffectApplicationService statusApplication,
        IStatusEffectCombatEventService statusCombat,
        IStatusEffectDurationService statusDuration,
        ITemporaryStatModifierService temporaryStatModifier,
        IAbilityImpactState ability,
        IPairedRangedTraitsService pairedRangedTraits,
        ICombatRecorderService combatRecorder,
        ICapstoneProofService capstoneProof,
        IResistanceService resistance,
        IPartyService party)
        : base(queries, resources, attackTiming, suppression, combatActivity, damageModifiers, repeatedTarget, abilityHit, stat, statusEffect, statusApplication,
            statusCombat, statusDuration, temporaryStatModifier, ability, pairedRangedTraits, combatRecorder, capstoneProof, resistance, party, FacadeForwarder.For<ICombatDamageTakenService>())
    {
        _log = log;
        _taken = taken;
    }

    protected override int ApplyDamageTakenModifiers(
        uint defender,
        int damage,
        uint attacker = NWScript.OBJECT_INVALID,
        CombatDamageType damageType = CombatDamageType.Physical,
        CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
        int targetStatusDamagePercentAdjustment = 0,
        bool isLandedAttack = true,
        bool typedLeadershipReductionAlreadyApplied = false)
    {
        _log.Add($"Taken {defender} {damage} {attacker} {damageType} {deliveryType} {targetStatusDamagePercentAdjustment} {isLandedAttack} {typedLeadershipReductionAlreadyApplied}");
        return _taken()(damage);
    }

    public int BaseApplyDamageTakenModifiers(uint defender, int damage) => base.ApplyDamageTakenModifiers(defender, damage);
}
