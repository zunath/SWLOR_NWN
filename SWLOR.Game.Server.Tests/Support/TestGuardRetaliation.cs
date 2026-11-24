#nullable enable
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

public sealed partial class TestDamageTaken : CombatDamageTakenService
{
    private readonly List<string> _log;
    private readonly Func<int> _calculated;

    public TestDamageTaken(
        List<string> log,
        Func<int> calculated,
        ICombatFeedbackService feedback,
        ICombatQueriesService queries,
        ICombatResourcesService resources,
        ICombatAttackTimingService attackTiming,
        ICombatDamageModifiersService damageModifiers,
        ICombatAbilityHitService abilityHit,
        ICombatHitEffectsService hitEffects,
        IStatService stat,
        ICombatRatingService combatRating,
        IStatusEffectService statusEffect,
        IStatusEffectCombatEventService statusCombat,
        ITemporaryStatModifierService temporaryStatModifier,
        ICombatRecorderService combatRecorder,
        IPvpCombatService pvpCombat,
        IEnmityService enmity,
        ICombatDefenseMemoryService defenseMemory)
        : base(feedback, queries, resources, attackTiming, damageModifiers, abilityHit, hitEffects, stat, combatRating,
            statusEffect, statusCombat, temporaryStatModifier, combatRecorder, pvpCombat, enmity,
            FacadeForwarder.For<ICombatDamageRollService>(), defenseMemory)
    {
        _log = log;
        _calculated = calculated;
    }

    protected override int CalculateDamage(
        int attackerAttack,
        int attackerDMG,
        int attackerStat,
        int defenderDefense,
        int defenderStat,
        int critical,
        int deltaCap = 0)
    {
        _log.Add($"Calculate {attackerAttack} {attackerDMG} {attackerStat} {defenderDefense} {defenderStat} {critical} {deltaCap}");
        return _calculated();
    }

    public int BaseCalculateDamage(int attack, int dmg, int attackStat, int defense, int defenseStat, int critical) =>
        base.CalculateDamage(attack, dmg, attackStat, defense, defenseStat, critical);
}
