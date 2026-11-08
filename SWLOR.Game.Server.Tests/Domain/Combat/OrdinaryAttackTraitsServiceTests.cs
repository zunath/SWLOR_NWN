using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ordinary-attack haste instance: the hit gates, the stacking and timer refresh, the target change, the reconcile
/// and clear paths. The engine is a recording fake, the clock is set by the test, and the unconverted stat and
/// status-effect calls are answered by a subclass that records them.
/// </summary>
[TestFixture]
public class OrdinaryAttackTraitsServiceTests
{
    private const uint Actor = 1001;
    private const uint Target = 2002;
    private const uint Invalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private DateTime _now;
    private bool _actorDead;
    private int _hitPoints;
    private bool _hostile;
    private TestTraits _traits = null!;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
        _actorDead = false;
        _hitPoints = 100;
        _hostile = true;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        _engine.SetReturnHandler("GetIsDead", _ => _actorDead);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => _hitPoints);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _hostile);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _traits = new TestTraits();
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    [Test]
    public void AHitThatPassesTheGatesGrantsAStackAndStartsTheEffect()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);

        _traits.RecordHit(Actor, Target, 1);

        _traits.Applied.Should().ContainSingle().Which.Should().Be((1, 5, 10f));
    }

    [Test]
    public void StacksGrowOnEachDistinctRollAndTheEffectIsReplaced()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);

        _traits.RecordHit(Actor, Target, 1);
        _traits.RecordHit(Actor, Target, 2);

        _traits.Applied.Select(entry => entry.Stacks).Should().Equal(1, 2);
        _traits.Removed.Should().Be(1, "the one-stack effect is replaced by the two-stack effect");
    }

    [Test]
    public void TheSameStacksKeepTheEffectAndRefreshTheTimer()
    {
        _traits.Configure(perStack: 5, maximum: 1, cooldown: 0, duration: 10);
        _traits.RecordHit(Actor, Target, 1);
        _now = _now.AddSeconds(4);

        _traits.RecordHit(Actor, Target, 2);

        _traits.Refreshed.Should().ContainSingle().Which.Should().BeApproximately(10f, 0.01f);
        _traits.Applied.Should().HaveCount(1, "the effect with the same stacks keeps running");
    }

    [Test]
    public void HitsThatFailTheGatesAreIgnored()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);

        _traits.RecordHit(Invalid, Target, 1);
        _traits.RecordHit(Actor, Invalid, 2);
        _traits.RecordHit(Actor, Actor, 3);
        _hostile = false;
        _traits.RecordHit(Actor, Target, 4);
        _hostile = true;
        _hitPoints = 0;
        _traits.RecordHit(Actor, Target, 5);
        _hitPoints = 100;
        _actorDead = true;
        _traits.RecordHit(Actor, Target, 6);

        _traits.Applied.Should().BeEmpty();
    }

    [Test]
    public void ATraitWithNoStacksNeverStartsTracking()
    {
        _traits.Configure(perStack: 5, maximum: 0, cooldown: 0, duration: 10);

        _traits.RecordHit(Actor, Target, 1);
        _traits.Reconcile(Actor);

        _traits.Applied.Should().BeEmpty();
    }

    [Test]
    public void ChangingTargetDropsTheStacks()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);
        _traits.RecordHit(Actor, Target, 1);

        _traits.RecordTargetChange(Actor, Target + 1);

        _traits.Removed.Should().Be(1);
        _traits.RecordTargetChange(Actor + 1, Target);
        _traits.Removed.Should().Be(1, "an actor with no state has nothing to drop");
    }

    [Test]
    public void ReconcileExpiresTheStacksAndHandlesDeadAndMissingActors()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);
        _traits.RecordHit(Actor, Target, 1);

        _traits.Reconcile(Actor + 7);
        _now = _now.AddSeconds(11);
        _traits.Reconcile(Actor);

        _traits.Removed.Should().Be(1, "the expired stacks remove the effect");

        _traits.RecordHit(Actor, Target, 2);
        _actorDead = true;
        _traits.Reconcile(Actor);
        _traits.Removed.Should().Be(2, "death resets the state and removes the effect");

        _actorDead = false;
        _traits.RecordHit(Actor, Target, 3);
        _engine.SetReturnHandler("GetIsObjectValid", _ => false);
        _traits.Reconcile(Actor);
        _traits.Removed.Should().Be(3, "an actor that no longer exists is cleared");
    }

    [Test]
    public void ClearActorAndForgetDestroyedDropTheState()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);
        _traits.RecordHit(Actor, Target, 1);

        _traits.ClearActor(Actor);
        _traits.Removed.Should().Be(1);
        _traits.Reconcile(Actor);
        _traits.Applied.Should().HaveCount(1, "the cleared actor is no longer tracked");

        _traits.RecordHit(Actor, Target, 9);
        _traits.ForgetDestroyed(Actor);
        _traits.Removed.Should().Be(1, "a destroyed actor's effect is not touched");
        _traits.Reconcile(Actor);
        _traits.Applied.Should().HaveCount(2);
    }

    [Test]
    public void ClearAndClearOnDeathRemoveTheEffect()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);
        _traits.RecordHit(Actor, Target, 1);

        _traits.Clear(Actor);
        _traits.ClearOnDeath(Actor);
        _traits.Clear(Actor + 1);
        _traits.ClearOnDeath(Actor + 1);

        _traits.Removed.Should().Be(4);
    }

    [Test]
    public void TheFacadeForwardsToTheContainersInstance()
    {
        _traits.Configure(perStack: 5, maximum: 3, cooldown: 0, duration: 10);
        using var container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IOrdinaryAttackTraitsService>(_traits)));
        using var _ = container.Install();

        OrdinaryAttackTraits.RecordHit(Actor, Target, 1);
        OrdinaryAttackTraits.RecordTargetChange(Actor, Target + 1);
        OrdinaryAttackTraits.Reconcile(Actor);
        OrdinaryAttackTraits.Clear(Actor);
        OrdinaryAttackTraits.ClearOnDeath(Actor);
        OrdinaryAttackTraits.ClearActor(Actor);
        OrdinaryAttackTraits.ForgetDestroyed(Actor);

        _traits.Applied.Should().ContainSingle();
    }

    private sealed class TestTraits : OrdinaryAttackTraitsService
    {
        public TestTraits()
            : base(FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IStatusEffectService>())
        {
        }

        private int _perStack, _maximum, _cooldown, _duration;
        private OrdinaryAttackHasteStatusEffect? _current;
        public readonly List<(int Stacks, int PerStack, float Seconds)> Applied = new();
        public readonly List<float> Refreshed = new();
        public int Removed;

        public void Configure(int perStack, int maximum, int cooldown, int duration)
        {
            _perStack = perStack;
            _maximum = maximum;
            _cooldown = cooldown;
            _duration = duration;
        }

        protected override int GetStatAdjustment(uint creature, StatType stat) => stat switch
        {
            StatType.OrdinaryAttackHasteRampPerStack => _perStack,
            StatType.OrdinaryAttackHasteRampMaximumStacks => _maximum,
            StatType.OrdinaryAttackHasteRampGrantCooldownSeconds => _cooldown,
            StatType.OrdinaryAttackHasteRampDurationSeconds => _duration,
            _ => 0
        };

        protected override OrdinaryAttackHasteStatusEffect GetHasteEffect(uint actor) => _current!;

        protected override void RemoveHasteEffect(uint actor)
        {
            Removed++;
            _current = null;
        }

        protected override void RefreshHasteEffectDuration(uint actor, float remaining) => Refreshed.Add(remaining);

        protected override void ApplyHasteEffect(uint actor, int stacks, int hastePerStack, float remaining)
        {
            Applied.Add((stacks, hastePerStack, remaining));
            _current = new OrdinaryAttackHasteStatusEffect(stacks, hastePerStack);
        }
    }
}
