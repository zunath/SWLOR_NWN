using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The two retarget notices the enmity split adds: what the leash and stealth tell the attack part, in what order, the loud failure
/// when nobody registered, and the one registration the attack part makes in each generation.
/// </summary>
[TestFixture]
public class EnmityRetargetNoticeTests
{
    private const uint Creature = 11;

    [Test]
    public void StealthHandsEachRetargetToEveryRegisteredListenerInRegistrationOrder()
    {
        var stealth = new ListenerStealth();
        var calls = new List<string>();
        stealth.RegisterRetargetListener(new RecordingListener("first", calls));
        stealth.RegisterRetargetListener(new RecordingListener("second", calls));

        stealth.Reevaluate(Creature);
        stealth.Attack(Creature);

        calls.Should().Equal($"first:reevaluate({Creature})", $"second:reevaluate({Creature})", $"first:attack({Creature})", $"second:attack({Creature})");
    }

    [Test]
    public void StealthKeepsOneEntryWhenTheSameListenerRegistersTwice()
    {
        var stealth = new ListenerStealth();
        var calls = new List<string>();
        var listener = new RecordingListener("only", calls);
        stealth.RegisterRetargetListener(listener);
        stealth.RegisterRetargetListener(listener);

        stealth.Reevaluate(Creature);

        calls.Should().ContainSingle();
    }

    [Test]
    public void StealthRetargetingWithNoListenerRegisteredFailsAtOnce()
    {
        var stealth = new ListenerStealth();

        var reevaluate = () => stealth.Reevaluate(Creature);
        var attack = () => stealth.Attack(Creature);

        reevaluate.Should().Throw<InvalidOperationException>().WithMessage("*retarget listener*");
        attack.Should().Throw<InvalidOperationException>().WithMessage("*retarget listener*");
    }

    [Test]
    public void TheLeashHandsTheRetargetToEveryRegisteredListenerInRegistrationOrder()
    {
        var leash = new ListenerAI();
        var calls = new List<string>();
        leash.RegisterRetargetListener(new RecordingListener("first", calls));
        leash.RegisterRetargetListener(new RecordingListener("second", calls));

        leash.Retarget(Creature);

        calls.Should().Equal($"first:attack({Creature})", $"second:attack({Creature})");
    }

    [Test]
    public void TheLeashKeepsOneEntryWhenTheSameListenerRegistersTwice()
    {
        var leash = new ListenerAI();
        var calls = new List<string>();
        var listener = new RecordingListener("only", calls);
        leash.RegisterRetargetListener(listener);
        leash.RegisterRetargetListener(listener);

        leash.Retarget(Creature);

        calls.Should().ContainSingle();
    }

    [Test]
    public void TheLeashRetargetingWithNoListenerRegisteredFailsAtOnce()
    {
        var leash = new ListenerAI();

        var retarget = () => leash.Retarget(Creature);

        retarget.Should().Throw<InvalidOperationException>().WithMessage("*retarget listener*");
    }

    [Test]
    public void TheAttackPartRegistersItselfWithTheLeashAndStealthAndAFurtherRegistrationKeepsOneEntryEach()
    {
        var leash = ScriptedProxy.Create<ICombatLeash>(out var leashScript);
        var leashListeners = new List<ILeashRetargetListener>();
        leashScript.On("RegisterRetargetListener", arguments =>
        {
            if (!leashListeners.Contains((ILeashRetargetListener)arguments[0]!))
                leashListeners.Add((ILeashRetargetListener)arguments[0]!);
            return null;
        });
        var stealth = ScriptedProxy.Create<IStealthService>(out var stealthScript);
        var stealthListeners = new List<IStealthRetargetListener>();
        stealthScript.On("RegisterRetargetListener", arguments =>
        {
            if (!stealthListeners.Contains((IStealthRetargetListener)arguments[0]!))
                stealthListeners.Add((IStealthRetargetListener)arguments[0]!);
            return null;
        });
        var attack = new EnmityAttackService(FacadeForwarder.For<IEnmityService>(), leash, FacadeForwarder.For<IActivityService>(), stealth,
            FacadeForwarder.For<ICompanionTargetQuery>(), FacadeForwarder.For<ICombatActivityService>(),
            FacadeForwarder.For<ICombatAttackTimingService>(), FacadeForwarder.For<ICombatQueriesService>());

        attack.RegisterRetargetListeners();
        attack.RegisterRetargetListeners();

        leashListeners.Should().ContainSingle().Which.Should().BeSameAs(attack);
        stealthListeners.Should().ContainSingle().Which.Should().BeSameAs(attack);
    }

    [Test]
    public void EachGenerationsContainerHoldsTheAttackPartOnceInBothListsAfterTheBootHandlerRuns()
    {
        for (var generation = 0; generation < 2; generation++)
        {
            using var facades = new FacadeContainer();
            var attack = facades.Get<IEnmityAttackService>();
            attack.RegisterRetargetListeners();
            attack.RegisterRetargetListeners();

            Listeners(facades.Get<IAIService>()).Should().ContainSingle().Which.Should().BeSameAs(attack);
            Listeners(facades.Get<IStealthService>()).Should().ContainSingle().Which.Should().BeSameAs(attack);
        }
    }

    private static IList<object> Listeners(object owner) =>
        ((System.Collections.IEnumerable)owner.GetType().GetField("_retargetListeners", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(owner)!).Cast<object>().ToList();

    private sealed class RecordingListener : ILeashRetargetListener, IStealthRetargetListener
    {
        private readonly string _name;
        private readonly List<string> _calls;

        public RecordingListener(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public void AttackHighestEnmityTarget(uint creature) => _calls.Add($"{_name}:attack({creature})");

        public void ReevaluateEnemyAttackTargets(uint creature) => _calls.Add($"{_name}:reevaluate({creature})");
    }

    /// <summary>The real stealth service with its two retarget seams exposed and nothing else overridden.</summary>
    private sealed class ListenerStealth : StealthService
    {
        public ListenerStealth()
            : base(FacadeForwarder.For<IPerkService>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>(),
                FacadeForwarder.LazyFor<IStatusEffectApplicationService>(), FacadeForwarder.For<IEnmityService>(),
                FacadeForwarder.LazyFor<IEspionageInfiltrationService>())
        {
        }

        public void Reevaluate(uint creature) => ReevaluateEnemyAttackTargets(creature);

        public void Attack(uint creature) => AttackHighestEnmityTarget(creature);
    }

    /// <summary>The real AI core with its retarget seam exposed and nothing else overridden.</summary>
    private sealed class ListenerAI : AIService
    {
        public ListenerAI()
            : base(FacadeForwarder.For<IPartyRoster>(), FacadeForwarder.For<ICompanionLookup>(), FacadeForwarder.For<IDMPresenceService>(),
                FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<ICreatureStatService>(),
                FacadeForwarder.For<IStatusEffectService>())
        {
        }

        public void Retarget(uint creature) => AttackHighestEnmityTarget(creature);
    }
}
