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

public sealed partial class TestTriggeredDamage : CombatTriggeredDamageService
{
    private readonly List<string> _log;
    private readonly Func<Func<int, int>> _taken;

    public TestTriggeredDamage(
        List<string> log,
        Func<Func<int, int>> taken,
        ICombatDamageModifiersService damageModifiers,
        ICombatDealtEffectsService dealtEffects,
        IStatService stat,
        IStatusEffectCombatEventService statusEffectCombatEvents,
        IAbilityImpactState impactState,
        ICombatRecorderService combatRecorder,
        IResistanceService resistance)
        : base(damageModifiers, FacadeForwarder.For<ICombatDamageTakenService>(), dealtEffects, stat, statusEffectCombatEvents, impactState,
            combatRecorder, resistance)
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
