using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Twin Pistol ability instance: validating and starting an activation, resolving a single volley, the multi-volley
/// channel with its delayed shots and cancel conditions, interruption and the destroyed-owner cleanup. The engine is a
/// recording fake with a small item world, the paired traits and mastery services are scripted, and the unconverted
/// ability, combat and native calls are answered by a subclass that records them.
/// </summary>
[TestFixture]
public class TwinPistolAbilityServiceTests
{
    private const uint Actor = 1;
    private const uint Target = 2;
    private const uint Right = 11;
    private const uint Left = 12;
    private const uint Area = 500;
    private const string ActivationId = "activation-1";

    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private ItemWorld _world = null!;
    private ScriptedProxy _traitsProxy = null!;
    private ScriptedProxy _masteryProxy = null!;
    private TestAbility _ability = null!;
    private static readonly TwinPistolAbilitySpec Single = new() { FlatPower = 4, MaxRange = 20f };

    // A volley pushes its weapon impact source through Combat, whose combat queries service holds the table.
    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _engine = new FakeNWScript();
        _world = new ItemWorld(_engine);
        _world.Add(Actor);
        _world.Add(Target);
        _world.Add(Right, baseItem: BaseItem.Pistol);
        _world.Add(Left, baseItem: BaseItem.CompanionPistol);
        _world.Equip(Actor, InventorySlot.RightHand, Right);
        _world.Equip(Actor, InventorySlot.LeftHand, Left);
        _engine.SetReturnHandler("GetIsDead", _ => false);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 100);
        _engine.SetReturnHandler("GetArea", _ => Area);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => true);
        _engine.SetReturnHandler("GetDistanceBetween", _ => 5f);
        _engine.SetReturnHandler("LineOfSightObject", _ => true);
        _engine.SetReturnHandler("GetCommandable", _ => true);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        var traits = ScriptedProxy.Create<IPairedRangedTraitsService>(out _traitsProxy);
        _traitsProxy.On("CompleteIntrinsicAbility", _ => null);
        var mastery = ScriptedProxy.Create<ITwinPistolMasteryService>(out _masteryProxy);
        _masteryProxy.On("RecordAbility", _ => null);
        _ability = new TestAbility(traits, mastery);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private static TwinPistolAbilitySpec Volley(int count) => new() { FlatPower = 4, VolleyCount = count, VolleyIntervalSeconds = 0.5f };

    private IReadOnlyList<RecordedCall> Delayed() => _engine.CallsTo("DelayCommand");

    [Test]
    public void ValidateNamesTheProblem()
    {
        _ability.Validate(Actor, Target, Single).Should().BeEmpty();

        _ability.Eligible.Clear();
        _ability.Validate(Actor, Target, Single).Should().Contain("eligible attack weapon");
        _ability.Eligible.Add(Right);

        _engine.SetReturnHandler("LineOfSightObject", _ => false);
        _ability.Validate(Actor, Target, Single).Should().Contain("hostile target");
        _engine.SetReturnHandler("LineOfSightObject", _ => true);
        _engine.SetReturnHandler("GetDistanceBetween", _ => 50f);
        _ability.Validate(Actor, Target, Single).Should().NotBeEmpty();
        _engine.SetReturnHandler("GetDistanceBetween", _ => 5f);
        _ability.Validate(Actor, Actor, Single).Should().NotBeEmpty("an actor is not its own target");
        _ability.Validate(Actor, ItemWorld.ObjectInvalid, Single).Should().NotBeEmpty();
    }

    [Test]
    public void BeginNeedsALegalTargetAndAnActiveActivation()
    {
        _ability.ActiveId = string.Empty;
        _ability.Begin(Actor, Target, Single).Should().BeFalse("there is no active activation to attach to");
        _ability.ActiveId = ActivationId;
        _ability.Begin(Actor, ItemWorld.ObjectInvalid, Single).Should().BeFalse();

        _ability.Begin(Actor, Target, Single).Should().BeTrue();

        _ability.HasActiveActivation(Actor).Should().BeTrue();
        _ability.ActiveId = "another";
        _ability.HasActiveActivation(Actor).Should().BeFalse("the active ability is a different activation");
        _ability.HasActiveActivation(Actor + 5).Should().BeFalse();
    }

    [Test]
    public void ASingleVolleyFiresBothHandsAndCompletesTheIntrinsics()
    {
        _ability.Begin(Actor, Target, Single);

        _ability.Complete(Actor, Single);

        _ability.Impacts.Should().HaveCount(2);
        _ability.Launches.Should().HaveCount(2);
        _traitsProxy.Calls.Should().ContainSingle(call => call.Member == "CompleteIntrinsicAbility");
        _masteryProxy.Calls.Should().ContainSingle(call => call.Member == "RecordAbility");
        _ability.AnimationsPlayed.Should().Be(1);
        _ability.HasActiveActivation(Actor).Should().BeFalse("the activation was consumed");
    }

    [Test]
    public void ACompletionForADifferentSpecOrAChangedLoadoutDoesNothing()
    {
        _ability.Begin(Actor, Target, Single);
        _ability.Complete(Actor, Volley(1));
        _ability.Impacts.Should().BeEmpty();

        _ability.Begin(Actor, Target, Single);
        _world.Equip(Actor, InventorySlot.LeftHand, ItemWorld.ObjectInvalid);
        _ability.Complete(Actor, Single);
        _ability.Impacts.Should().BeEmpty();
        _ability.Complete(Actor, Single);
    }

    [Test]
    public void ADeadTargetStopsTheVolleyAndADeadActorSkipsTheIntrinsics()
    {
        _ability.Begin(Actor, Target, Single);
        _ability.HitOnImpact = true;
        _ability.KillTargetOnImpact = () => _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Target);

        _ability.Complete(Actor, Single);

        _ability.Impacts.Should().HaveCount(1, "the second hand has no living target to shoot");

        _engine.SetReturnHandler("GetIsDead", _ => false);
        _ability.Begin(Actor, Target, Single);
        _ability.KillTargetOnImpact = () => _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Actor);
        var before = _traitsProxy.Calls.Count;
        _ability.Complete(Actor, Single);
        _traitsProxy.Calls.Count.Should().Be(before, "a dead actor completes nothing");
    }

    [Test]
    public void ALandedHitIsReportedToTheLaunchPresentation()
    {
        _ability.Begin(Actor, Target, Single);
        _ability.HitOnImpact = true;

        _ability.Complete(Actor, Single);

        _ability.Launches.Should().OnlyContain(launch => launch.Hit);
        _traitsProxy.Calls.Single().Arguments[2].Should().Be(true);
    }

    [Test]
    public void AChannelFiresTheFirstShotAtOnceAndTheRestOnTheirDelays()
    {
        var spec = Volley(3);
        _ability.Begin(Actor, Target, spec);

        _ability.Complete(Actor, spec);

        _ability.HasActiveActivation(Actor).Should().BeTrue("the channel keeps the activation alive");
        _ability.Impacts.Should().HaveCount(2, "the first volley fired both hands");
        _ability.DeferredStart.Should().Be(1);
        _ability.PairedCycleCancelled.Should().BeTrue();
        _ability.PendingHandCancelled.Should().BeTrue();
        Delayed().Should().HaveCount(2);
        Delayed().Select(call => (float)call.Arguments[0]!).Should().Equal(0.5f, 1f);

        ((Action)Delayed()[0].Arguments[1]!)();
        ((Action)Delayed()[1].Arguments[1]!)();

        _ability.Impacts.Should().HaveCount(6);
        _ability.HasActiveActivation(Actor).Should().BeFalse("the channel finished");
        _ability.DeferredComplete.Should().Be(1);
        _traitsProxy.Calls.Should().ContainSingle(call => call.Member == "CompleteIntrinsicAbility");
        _ability.AnimationsPlayed.Should().Be(3, "each volley of a non-paired grip plays its own gesture");
    }

    [Test]
    public void PairedEquipmentOwnsTheChannelClip()
    {
        var spec = Volley(2);
        _ability.AnimationCompatible = true;
        _ability.Begin(Actor, Target, spec);

        _ability.Complete(Actor, spec);

        _ability.AnimationsPlayed.Should().Be(0);
    }

    [Test]
    public void AChannelStopsWhenTheTargetOrTheActorCanNoLongerFire()
    {
        var spec = Volley(3);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _engine.SetReturnHandler("LineOfSightObject", _ => false);

        ((Action)Delayed()[0].Arguments[1]!)();

        _ability.Interrupts.Should().NotBeEmpty();
        _ability.HasActiveActivation(Actor).Should().BeFalse();
        _ability.Impacts.Should().HaveCount(2, "the cancelled shot never fired");
        ((Action)Delayed()[1].Arguments[1]!)();
        _ability.Impacts.Should().HaveCount(2, "a later shot of a finished channel is ignored");
    }

    [Test]
    public void AChannelCancelsWhenTheActorIsIncapacitatedOrHasADisablingEffect()
    {
        var spec = Volley(3);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _ability.Incapacitated = true;
        ((Action)Delayed()[0].Arguments[1]!)();
        _ability.HasActiveActivation(Actor).Should().BeFalse();

        _engine.ClearCalls();
        _ability.Incapacitated = false;
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _engine.SetReturnHandler("GetCommandable", _ => false);
        ((Action)Delayed()[0].Arguments[1]!)();
        _ability.HasActiveActivation(Actor).Should().BeFalse();

        _engine.ClearCalls();
        _engine.SetReturnHandler("GetCommandable", _ => true);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _engine.SetReturnSequence("GetIsEffectValid", true, false);
        _engine.SetReturnHandler("GetEffectType", _ => EffectTypeScript.Stunned);
        ((Action)Delayed()[0].Arguments[1]!)();
        _ability.HasActiveActivation(Actor).Should().BeFalse();
    }

    [Test]
    public void AnEffectThatDoesNotDisableLeavesTheChannelRunning()
    {
        var spec = Volley(2);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _engine.SetReturnSequence("GetIsEffectValid", true, false);
        _engine.SetReturnHandler("GetEffectType", _ => EffectTypeScript.Haste);

        ((Action)Delayed()[0].Arguments[1]!)();

        _ability.Impacts.Should().HaveCount(4);
    }

    [Test]
    public void AShotThatThrowsCancelsTheChannelAndRethrows()
    {
        var spec = Volley(2);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _ability.ThrowOnImpact = true;

        var act = () => ((Action)Delayed()[0].Arguments[1]!)();

        act.Should().Throw<InvalidOperationException>();
        _ability.HasActiveActivation(Actor).Should().BeFalse();
    }

    [Test]
    public void CancelInterruptsTheActivationAndTheChannelWithTheCause()
    {
        var spec = Volley(2);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _ability.Begin(Actor, Target, Single);

        _ability.Cancel(Actor, CombatRecordInterruptCause.Moved);

        _ability.Interrupts.Should().HaveCount(2);
        _ability.Interrupts.Should().OnlyContain(interrupt => interrupt == CombatRecordInterruptCause.Moved);
        _ability.HasActiveActivation(Actor).Should().BeFalse();
        _ability.DeferredComplete.Should().Be(1);
    }

    [Test]
    public void CancelOfADeadActorRecordsADeath()
    {
        _ability.Begin(Actor, Target, Single);
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Actor);

        _ability.Cancel(Actor);

        _ability.Interrupts.Should().Equal(CombatRecordInterruptCause.Died);
    }

    [Test]
    public void CancelOfALivingActorKeepsTheGivenCauseAndSkipsBlankActivations()
    {
        _ability.Cancel(Actor);
        _ability.Interrupts.Should().BeEmpty();

        _ability.Begin(Actor, Target, Single);
        _ability.Cancel(Actor);
        _ability.Interrupts.Should().Equal(CombatRecordInterruptCause.Other);
    }

    [Test]
    public void ADestroyedOwnerLosesItsStateWithoutBeingTouched()
    {
        var spec = Volley(2);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);
        _ability.Begin(Actor, Target, Single);

        _ability.ForgetDestroyed(Actor);

        _ability.DeferredComplete.Should().Be(1, "the channel's deferred stamina-cost context is released");
        _ability.HasActiveActivation(Actor).Should().BeFalse();
        _ability.Interrupts.Should().BeEmpty();
        _ability.ForgetDestroyed(Actor);
    }

    [Test]
    public void InterruptChannelFinishesARunningChannelOnly()
    {
        _ability.InterruptChannel(Actor);
        var spec = Volley(3);
        _ability.Begin(Actor, Target, spec);
        _ability.Complete(Actor, spec);

        _ability.InterruptChannel(Actor);

        _ability.HasActiveActivation(Actor).Should().BeFalse();
        _traitsProxy.Calls.Should().ContainSingle(call => call.Member == "CompleteIntrinsicAbility");
    }

    [Test]
    public void ARemovedWeaponShrinksTheEligibleSetAndDuplicatesCountOnce()
    {
        _world.Equip(Actor, InventorySlot.LeftHand, Right);
        _ability.Eligible.Clear();
        _ability.Eligible.Add(Right);

        _ability.GetEligibleWeapons(Actor).Should().Equal(Right);
    }

    private sealed class TestAbility : TwinPistolAbilityService
    {
        public readonly HashSet<uint> Eligible = new() { Right, Left };
        public readonly List<CombatRecordInterruptCause> Interrupts = new();
        public readonly List<(uint Weapon, bool Hit)> Launches = new();
        public readonly List<(uint Actor, uint Target)> Impacts = new();
        public string ActiveId = ActivationId;
        public int AnimationsPlayed;
        public int DeferredStart;
        public int DeferredComplete;
        public bool PairedCycleCancelled;
        public bool PendingHandCancelled;
        public bool HitOnImpact;
        public bool Incapacitated;
        public bool AnimationCompatible;
        public bool ThrowOnImpact;
        public Action? KillTargetOnImpact;
        private readonly AbilityDetail _detail = new() { EffectiveLevelPerkType = PerkType.TempoShot };

        public TestAbility(IPairedRangedTraitsService traits, ITwinPistolMasteryService mastery)
            : base(traits, mastery, FacadeForwarder.For<IAbilityService>(), FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<ICombatResourcesService>())
        {
        }

        protected override string GetActiveAbilityActivationId(uint actor) => ActiveId;

        protected override WeaponAbilityTraitActivation GetActiveWeaponTraits(uint actor) =>
            new(new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0), 0);

        protected override bool InterruptAbilityActivation(uint actor, string activationId, CombatRecordInterruptCause cause)
        {
            Interrupts.Add(cause);
            return true;
        }

        protected override bool CanItemTriggerWeaponAbility(uint item) => Eligible.Contains(item);

        protected override int GetAbilityStaminaPaid(uint actor, AbilityDetail ability) => 6;

        protected override void DeferAbilityStaminaCostContext(uint actor, AbilityDetail ability) => DeferredStart++;

        protected override void CompleteDeferredAbilityStaminaCostContext(uint actor, AbilityDetail ability) => DeferredComplete++;

        protected override AbilityDetail GetActiveAbility(uint actor) => _detail;

        protected override Action<int> CaptureRepeatedAbilityImpact(uint actor, Action<int> impactAction, int baseDamage) => impactAction;

        protected override void PlayAbilityImpactAnimation(uint actor) => AnimationsPlayed++;

        protected override int ApplyCombatImpact(uint actor, uint target, Location targetLocation, int baseDamage,
            CombatDamageType damageType, Action<uint> afterSuccessfulHit, AbilityType damageAbility)
        {
            if (ThrowOnImpact) throw new InvalidOperationException("boom");
            Impacts.Add((actor, target));
            if (HitOnImpact) afterSuccessfulHit(target);
            KillTargetOnImpact?.Invoke();
            return 0;
        }

        protected override bool HasStatusEffectCategory(uint actor, StatusEffectCategory category) => Incapacitated;

        protected override void CancelPairedRangedAttackCycle(uint actor) => PairedCycleCancelled = true;

        protected override void CancelPendingHand(uint actor) => PendingHandCancelled = true;

        protected override void OnAbilityWeaponLaunch(uint actor, uint target, uint weapon, bool hit) => Launches.Add((weapon, hit));

        protected override bool IsAnimationEquipmentCompatible(AnimationEquipmentRequirement requirement, uint actor) => AnimationCompatible;

        protected override WeaponShotProfile CaptureShotProfile(uint actor, uint weapon, AbilityType damageAbilityOverride) =>
            new(weapon, SkillType.TwinPistol, 10, CombatDamageType.Physical, 0, 0, damageAbilityOverride,
                ((int)(weapon == Right ? BaseItem.Pistol : BaseItem.CompanionPistol)).ToString());
    }
}
