using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The state owner list of the combat cleanup: the phases are the old order of the forgets, the container registers every service
/// that keeps state by creature in the order the cleanup needs, the cleanup walks the phases from the first to the last with the owners
/// that share a phase in registration order and clears the temporary stat modifiers last, and an empty list fails loudly.
/// </summary>
[TestFixture]
public class CombatStateOwnerTests
{
    private const uint Creature = 33;

    [Test]
    public void ThePhasesAreTheOldOrderOfTheForgetsNumberedOneToEleven()
    {
        Enum.GetValues<CombatForgetPhase>().Select(phase => (int)phase).Should().Equal(Enumerable.Range(1, 11));
        Enum.GetValues<CombatForgetPhase>().Select(phase => phase.ToString()).Should().Equal(
            "ResourceTriggers", "Activity", "DefenseMemory", "Suppression", "Activation", "HitEffects", "AbilityImpact",
            "OpeningActivity", "AbilityStaminaCost", "AttackTiming", "DamageModifiers");
    }

    [Test]
    public void TheContainerRegistersEveryStateOwnerInTheOrderTheCleanupNeeds()
    {
        using var facades = new FacadeContainer();

        facades.Get<IEnumerable<ICombatCreatureStateOwner>>().Select(owner => owner.GetType()).Should().Equal(
            typeof(CombatResourcesService), typeof(CombatActivityService), typeof(CombatDefenseMemoryService),
            typeof(CombatSuppressionService), typeof(CombatAbilityOffenseEffectsService),
            typeof(CombatCriticalHitEffectsService), typeof(CombatHitEffectsService),
            typeof(CombatAbilityHitRidersService), typeof(CombatAbilityAreaImpactService),
            typeof(CombatAttackTimingService), typeof(CombatRateModifiersService), typeof(CombatRepeatedTargetService));
    }

    [Test]
    public void EveryClassThatImplementsTheOwnerInterfaceIsRegisteredOnceAndSharesItsInstanceWithItsService()
    {
        using var facades = new FacadeContainer();
        var registered = facades.Get<IEnumerable<ICombatCreatureStateOwner>>().ToArray();
        var implementations = typeof(CombatService).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ICombatCreatureStateOwner).IsAssignableFrom(type))
            .ToArray();

        registered.Select(owner => owner.GetType()).Should().BeEquivalentTo(implementations);
        registered.Should().OnlyHaveUniqueItems();
        registered[0].Should().BeSameAs(facades.Get<ICombatResourcesService>());
        registered[6].Should().BeSameAs(facades.Get<ICombatHitEffectsService>());
        registered[11].Should().BeSameAs(facades.Get<ICombatRepeatedTargetService>());
    }

    [Test]
    public void TheCleanupWalksThePhasesInOrderWithTheOwnersOfASharedPhaseInRegistrationOrderAndClearsTheModifiersLast()
    {
        var log = new List<string>();
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! == Creature);
        engine.SetReturn("GetExitingObject", Creature);
        using var scope = NWScriptRuntime.Override(engine);
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls.On(nameof(ITemporaryStatModifierService.Clear), args =>
        {
            log.Add($"clear {args[0]}");
            return null;
        });
        var service = Build(new ICombatCreatureStateOwner[]
        {
            new Spy("late", log, CombatForgetPhase.DamageModifiers, CombatForgetPhase.Activity),
            new Spy("middle", log, CombatForgetPhase.AbilityImpact),
            new Spy("first", log, CombatForgetPhase.ResourceTriggers, CombatForgetPhase.Activity),
            new Spy("second", log, CombatForgetPhase.AbilityImpact, CombatForgetPhase.Activity),
        }, temporary);

        service.ClearStatTriggerState();

        log.Should().Equal(
            $"first {CombatForgetPhase.ResourceTriggers}",
            $"late {CombatForgetPhase.Activity}",
            $"first {CombatForgetPhase.Activity}",
            $"second {CombatForgetPhase.Activity}",
            $"middle {CombatForgetPhase.AbilityImpact}",
            $"second {CombatForgetPhase.AbilityImpact}",
            $"late {CombatForgetPhase.DamageModifiers}",
            $"clear {Creature}");
    }

    [Test]
    public void TheCleanupGivesEveryOwnerEveryPhaseOnceForTheCreature()
    {
        var log = new List<string>();
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! == Creature);
        engine.SetReturn("GetExitingObject", Creature);
        using var scope = NWScriptRuntime.Override(engine);
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls.On(nameof(ITemporaryStatModifierService.Clear), _ => null);
        var all = Enum.GetValues<CombatForgetPhase>();
        var service = Build(new ICombatCreatureStateOwner[] { new Spy("only", log, all) }, temporary);

        service.ClearStatTriggerState();

        log.Should().Equal(all.Select(phase => $"only {phase}"));
    }

    [Test]
    public void AnEmptyOwnerListFailsLoudlyWhereTheCleanupWouldForgetNothing()
    {
        var act = () => Build(Array.Empty<ICombatCreatureStateOwner>(), ScriptedProxy.Create<ITemporaryStatModifierService>(out _));

        act.Should().Throw<InvalidOperationException>().WithMessage("No combat creature state owner is registered*");
    }

    private static CombatService Build(IEnumerable<ICombatCreatureStateOwner> owners, ITemporaryStatModifierService temporary) =>
        new(owners,
            ScriptedProxy.Create<ICombatSuppressionService>(out _),
            ScriptedProxy.Create<ICombatActivityService>(out _),
            ScriptedProxy.Create<ICombatRateModifiersService>(out _),
            ScriptedProxy.Create<ICombatRepeatedTargetService>(out _),
            ScriptedProxy.Create<ICombatDefeatedEnemyEffectsService>(out _),
            ScriptedProxy.Create<IStatService>(out _),
            ScriptedProxy.Create<IStatusEffectService>(out _),
            temporary);

    private sealed class Spy : ICombatCreatureStateOwner
    {
        private readonly string _name;
        private readonly List<string> _log;
        private readonly HashSet<CombatForgetPhase> _owned;

        public Spy(string name, List<string> log, params CombatForgetPhase[] owned)
        {
            _name = name;
            _log = log;
            _owned = owned.ToHashSet();
        }

        public void ForgetCreatureState(uint creature, CombatForgetPhase phase)
        {
            if (_owned.Contains(phase))
                _log.Add($"{_name} {phase}");
        }
    }
}
