using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The activation path of <c>UsePerkFeat</c> over the real ability service, with the steps a definition can observe
/// recorded in one list: validation, the activation action, the costs, the impact and the recast. A creature that is not a
/// player runs it, so no step needs the database; the engine fake answers the checks.
/// </summary>
[TestFixture]
public class AbilityActivationPathTests
{
    private const uint Caster = 901;
    private const uint Target = 902;
    private const uint Weapon = 950;
    private const FeatType Feat = FeatType.Flash1;

    private readonly List<string> _log = new();
    private FacadeContainer _facades = null!;
    private IDisposable _scope = null!;
    private IDisposable _engineScope = null!;
    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private Dictionary<FeatType, AbilityDetail> _catalog = null!;

    private sealed class Cost : IAbilityActivationRequirement
    {
        private readonly List<string> _log;

        public Cost(List<string> log)
        {
            _log = log;
        }

        public string CheckRequirements(uint player, AbilityDetail ability = null!)
        {
            _log.Add("Requirement.Check");
            return string.Empty;
        }

        public void AfterActivationAction(uint player, AbilityDetail ability = null!) => _log.Add("Requirement.Pay");
    }

    [SetUp]
    public void SetUp()
    {
        _log.Clear();
        _facades = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton(Recast()));
            services.Replace(ServiceDescriptor.Singleton(Activity()));
            services.Replace(ServiceDescriptor.Singleton(Companions()));
        });
        _scope = _facades.Install();
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is Caster or Target or Weapon);
        _engine.SetReturn("GetCurrentHitPoints", 50);
        _engine.SetReturn("GetCommandable", true);
        _engine.SetReturn("LineOfSightObject", true);
        _engine.SetReturn("LineOfSightVector", true);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => (uint)args[0]! == Target && (uint)args[1]! == Caster);
        _engine.SetReturn("GetHasFeat", true);
        _engineScope = NWScriptRuntime.Override(_engine);
        _catalog = (Dictionary<FeatType, AbilityDetail>)Ability.GetAllAbilityDetails();
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
        _scope.Dispose();
        _facades.Dispose();
    }

    private IRecastService Recast()
    {
        var recast = ScriptedProxy.Create<IRecastService>(out var proxy);
        proxy.On("IsOnRecastDelay", args =>
            {
                _log.Add("IsOnRecastDelay");
                return (false, string.Empty);
            })
            .On("ApplyRecastDelay", args =>
            {
                _log.Add($"ApplyRecastDelay:{args[2]}");
                return null;
            });
        return recast;
    }

    private IActivityService Activity()
    {
        var activity = ScriptedProxy.Create<IActivityService>(out var proxy);
        proxy.On("IsBusy", args => false)
            .On("SetBusy", args =>
            {
                _log.Add("SetBusy");
                return null;
            })
            .On("ClearBusy", args =>
            {
                _log.Add("ClearBusy");
                return null;
            });
        return activity;
    }

    /// <summary>
    /// The caster is a companion: its resume-attack target comes from the companion service, not from the native action
    /// queue a unit test cannot read.
    /// </summary>
    private ICompanionControlService Companions()
    {
        var companions = ScriptedProxy.Create<ICompanionControlService>(out var proxy);
        proxy.On("IsRegisteredCompanion", args => (uint)args[0]! == Caster)
            .On("PeekAuthorizedTarget", args => AbilityBench.ObjectInvalid)
            .On("TryProcessPendingDefensiveReaction", args => false)
            .On("ResumeModePosition", args => null)
            .On("IsHostileAbilityTargetAuthorized", args => true);
        return companions;
    }

    private AbilityDetail Ability_(AbilityActivationType type, float delay = 0f) => new()
    {
        Name = "Probe",
        ActivationType = type,
        EffectiveLevelPerkType = PerkType.Invalid,
        SkillType = SkillType.Force,
        MaxRange = 30f,
        Requirements = new List<IAbilityActivationRequirement> { new Cost(_log) },
        CustomValidation = (_, _, _, _) =>
        {
            _log.Add("CustomValidation");
            return string.Empty;
        },
        ActivationDelay = (_, _, _) =>
        {
            _log.Add("ActivationDelay");
            return delay;
        },
        RecastDelay = _ =>
        {
            _log.Add("RecastDelay");
            return 12f;
        },
        ActivationAction = (_, _, _, _) =>
        {
            _log.Add("ActivationAction");
            return true;
        },
        ImpactAction = (activator, _, _, _) =>
        {
            _log.Add("Impact");
            Ability.GetActiveAbilityImpactSummary(activator).Should().NotBeNull("the impact is tracked while it runs");
        }
    };

    private void RunDelayed(float seconds)
    {
        var call = _engine.CallsTo("DelayCommand").Last(entry => (float)entry.Arguments[0]! == seconds);
        ((Action)call.Arguments[1]!)();
    }

    [Test]
    public void A_cast_validates_acts_then_waits_validates_again_pays_hits_and_starts_its_recast()
    {
        _catalog[Feat] = Ability_(AbilityActivationType.Casted, delay: 2f);

        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();

        _log.Should().Equal(
            "Requirement.Check",
            "CustomValidation",
            "IsOnRecastDelay",
            "ActivationDelay",
            "RecastDelay",
            "ActivationAction",
            "SetBusy");
        Ability.TryGetActiveAbilityActivation(Caster, out var active).Should().BeTrue();
        active.Ability.Should().BeSameAs(_catalog[Feat]);
        UsePerkFeat.GetActiveAbilityActivationId(Caster).Should().Be(active.ActivationId);
        _log.Clear();

        RunDelayed(2f);

        _log.Should().Equal(
            "ClearBusy",
            "Requirement.Check",
            "CustomValidation",
            "IsOnRecastDelay",
            "Requirement.Pay",
            "Impact",
            "ApplyRecastDelay:12");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse("the completed activation is released");
        Ability.GetLastCompletedAbilityImpactSummary(Caster).Should().NotBeNull();
    }

    [Test]
    public void A_cast_refused_at_completion_never_pays_or_hits()
    {
        var ability = Ability_(AbilityActivationType.Casted, delay: 2f);
        _catalog[Feat] = ability;
        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();
        _log.Clear();
        ability.CustomValidation = (_, _, _, _) => "Not any more.";

        RunDelayed(2f);

        _log.Should().Equal("ClearBusy", "Requirement.Check");
        Ability.GetLastActivationDenialReason().Should().Be("Not any more.");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse();
    }

    [Test]
    public void An_action_that_declines_ends_the_activation_with_no_cost_or_recast()
    {
        var ability = Ability_(AbilityActivationType.Casted);
        ability.ActivationAction = (_, _, _, _) =>
        {
            _log.Add("ActivationAction");
            return false;
        };
        _catalog[Feat] = ability;

        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();

        _log.Should().EndWith("ActivationAction");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse();
    }

    [Test]
    public void A_channel_pays_hits_and_starts_its_recast_before_it_waits()
    {
        var ability = Ability_(AbilityActivationType.Casted, delay: 3f);
        ability.IsChanneled = true;
        _catalog[Feat] = ability;

        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();

        _log.Should().Equal(
            "Requirement.Check",
            "CustomValidation",
            "IsOnRecastDelay",
            "ActivationDelay",
            "RecastDelay",
            "ActivationAction",
            "Requirement.Pay",
            "Impact",
            "ApplyRecastDelay:12",
            "SetBusy");
        _log.Clear();

        RunDelayed(3f);

        _log.Should().Equal("ClearBusy");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse();
    }

    [Test]
    public void An_interrupted_cast_stops_before_its_completion()
    {
        var ability = Ability_(AbilityActivationType.Casted, delay: 2f);
        _catalog[Feat] = ability;
        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();
        _log.Clear();

        UsePerkFeat.InterruptAbilityActivation(Caster).Should().BeTrue();
        UsePerkFeat.InterruptAbilityActivation(Caster).Should().BeFalse("it is already released");
        _log.Should().Equal("ClearBusy");
        _log.Clear();

        RunDelayed(2f);

        _log.Should().BeEmpty();
    }

    [Test]
    public void A_reload_interrupts_a_cast_in_progress_so_the_caster_is_not_left_busy()
    {
        // P4-15: the closure that would finish the cast and clear the busy flag is dropped by the reload.
        _catalog[Feat] = Ability_(AbilityActivationType.Casted, delay: 2f);
        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();
        _log.Clear();

        ((SWLOR.Game.Server.Domain.Abilities.AbilityService)Ability).QuiesceForReload(Array.Empty<uint>());

        _log.Should().Equal("ClearBusy");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse("the new build has no activation to finish or interrupt");
    }

    [Test]
    public void A_reload_interrupts_an_impact_still_waiting_on_its_delay()
    {
        var ability = Ability_(AbilityActivationType.Casted);
        ability.ImpactDelay = 1.5f;
        _catalog[Feat] = ability;
        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();
        RunDelayed(0f);
        _log.Should().EndWith("SetBusy", "the caster waits on the impact");
        _log.Clear();

        ((SWLOR.Game.Server.Domain.Abilities.AbilityService)Ability).QuiesceForReload(Array.Empty<uint>());

        _log.Should().Contain("ClearBusy");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse();
        _log.Clear();
        RunDelayed(1.5f);
        _log.Should().NotContain("Impact", "an interrupted impact never lands");
    }

    [Test]
    public void A_reload_leaves_a_creature_without_a_cast_alone()
    {
        _catalog[Feat] = Ability_(AbilityActivationType.Casted, delay: 2f);
        _log.Clear();

        ((SWLOR.Game.Server.Domain.Abilities.AbilityService)Ability).QuiesceForReload(new[] { Caster });

        _log.Should().BeEmpty("only a cast clears a busy flag; any other activity keeps its own");
    }

    [Test]
    public void A_delayed_impact_starts_its_recast_first_and_hits_after_its_own_delay()
    {
        var ability = Ability_(AbilityActivationType.Casted);
        ability.ImpactDelay = 1.5f;
        _catalog[Feat] = ability;
        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();
        _log.Clear();

        RunDelayed(0f);

        _log.Should().Equal(
            "ClearBusy",
            "Requirement.Check",
            "CustomValidation",
            "IsOnRecastDelay",
            "Requirement.Pay",
            "ApplyRecastDelay:12",
            "SetBusy");
        _log.Clear();

        RunDelayed(1.5f);

        _log.Should().Equal("Impact", "ClearBusy");
        Ability.TryGetActiveAbilityActivation(Caster, out _).Should().BeFalse();
    }

    [Test]
    public void A_weapon_ability_pays_and_starts_its_recast_when_queued_and_hits_on_the_next_landed_attack()
    {
        _catalog[Feat] = Ability_(AbilityActivationType.Weapon);
        _engine.SetReturn("GetBaseItemType", (int)BaseItem.Longsword);

        UsePerkFeat.TryUseAbility(Caster, Target, Feat, Nowhere()).Should().BeTrue();

        _log.Should().Equal(
            "Requirement.Check",
            "CustomValidation",
            "IsOnRecastDelay",
            "ActivationAction",
            "Requirement.Pay",
            "RecastDelay",
            "ApplyRecastDelay:12");
        UsePerkFeat.IsWeaponAbilityQueued(Caster, Feat).Should().BeTrue();
        _log.Clear();

        UsePerkFeat.ReserveQueuedWeaponAbilityAttack(Caster, 0, Weapon);
        Ability.TryGetQueuedWeaponAttack(Caster, out var reservation).Should().BeTrue();
        reservation.Weapon.Should().Be(Weapon);
        UsePerkFeat.ReserveQueuedWeaponAbilityAttack(Caster, 1, Weapon + 1);
        Ability.TryGetQueuedWeaponAttack(Caster, out reservation).Should().BeTrue();
        reservation.AttackIndex.Should().Be(0, "the first landed roll keeps its reservation");

        UsePerkFeat.ResolveQueuedWeaponAbilityHit(Caster, Target, Weapon + 1);
        _log.Should().BeEmpty("another hand's item cannot consume the reservation");

        UsePerkFeat.ResolveQueuedWeaponAbilityHit(Caster, Target, Weapon);

        _log.Should().Equal("Impact");
        UsePerkFeat.IsWeaponAbilityQueued(Caster, Feat).Should().BeFalse();
        Ability.TryGetQueuedWeaponAttack(Caster, out _).Should().BeFalse();
    }

    private static Location Nowhere() => EngineStructures.Create<Location>(0);
}
