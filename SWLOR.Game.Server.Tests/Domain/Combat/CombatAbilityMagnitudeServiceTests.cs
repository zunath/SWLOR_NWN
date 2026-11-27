using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The magnitude part of the ability split (part 4, <c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) over a real impact
/// state store, with the perk and stat services it calls through its seams scripted: Force affinity scales only a tracked
/// Force impact's magnitude, and combat readiness scales a positive amount, for an activated ability only while it is tracked.
/// </summary>
[TestFixture]
public class CombatAbilityMagnitudeServiceTests
{
    private const uint Caster = 701;
    private const uint Other = 702;

    private readonly List<string> _log = new();
    private AbilityImpactStore _store = null!;
    private CombatAbilityMagnitudeService _service = null!;
    private int _readiness;
    private IDisposable _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _log.Clear();
        _readiness = 0;
        _engine = SWLOR.NWN.API.NWScript.NWScriptRuntime.Override(new FakeNWScript());
        var perk = ScriptedProxy.Create<IPerkForceAffinityService>(out var perkCalls);
        perkCalls
            .On(nameof(IPerkForceAffinityService.GetForceAffinityMagnitudeMultiplier), args =>
            {
                _log.Add($"Multiplier {args[0]} {args[1]}");
                return 1.5f;
            })
            .On(nameof(IPerkForceAffinityService.GetForceAffinityRestorativeMagnitudeMultiplier), args =>
            {
                _log.Add($"RestorativeMultiplier {args[0]} {args[1]}");
                return 1.25f;
            })
            .On(nameof(IPerkForceAffinityService.ApplyForceAffinityRestorativeMagnitude), args =>
            {
                _log.Add($"Restorative {args[0]} {args[1]} {args[2]}");
                return (int)args[2]! + 1;
            })
            .On(nameof(IPerkForceAffinityService.ApplyForceAffinityMagnitude), args =>
            {
                _log.Add($"Magnitude {args[0]} {args[1]} {args[2]}");
                return args[2] is float value ? (object)(value + 0.5f) : (int)args[2]! + 2;
            });
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetCombatReadinessPercent), args =>
        {
            _log.Add($"Readiness {args[0]}");
            return _readiness;
        });
        _store = new AbilityImpactStore();
        _service = new CombatAbilityMagnitudeService(_store, perk, stat);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    private void Track(SkillType skill) =>
        _store.Track(Caster, new AbilityImpactTracker(new AbilityDetail { SkillType = skill, EffectiveLevelPerkType = PerkType.ForceLeap },
            0, 0, 0, 0, 0, true, 0, null!, null!));

    [Test]
    public void Force_affinity_scales_only_a_tracked_force_impact()
    {
        _service.GetActiveForceAffinityMagnitudeMultiplier(Caster).Should().Be(1f);
        _service.ApplyActiveForceAffinityMagnitude(Caster, 10).Should().Be(10);
        Track(SkillType.Vibroblade);
        _service.GetActiveForceAffinityRestorativeMagnitudeMultiplier(Caster).Should().Be(1f);
        _service.ApplyActiveForceAffinityRestorativeMagnitude(Caster, 10).Should().Be(10);
        _service.ApplyActiveForceAffinityMagnitude(Caster, 10f).Should().Be(10f);
        _log.Should().BeEmpty("no Force impact is tracked");

        Track(SkillType.Force);

        _service.GetActiveForceAffinityMagnitudeMultiplier(Caster).Should().Be(1.5f);
        _service.GetActiveForceAffinityRestorativeMagnitudeMultiplier(Caster).Should().Be(1.25f);
        _service.ApplyActiveForceAffinityRestorativeMagnitude(Caster, 10).Should().Be(11);
        _service.ApplyActiveForceAffinityMagnitude(Caster, 10).Should().Be(12);
        _service.ApplyActiveForceAffinityMagnitude(Caster, 10f).Should().Be(10.5f);
        _service.ApplyActiveForceAffinityMagnitude(Other, 10).Should().Be(10, "the other creature tracks no impact");
        _log.Should().Equal(
            $"Multiplier {Caster} {PerkType.ForceLeap}",
            $"RestorativeMultiplier {Caster} {PerkType.ForceLeap}",
            $"Restorative {Caster} {PerkType.ForceLeap} 10",
            $"Magnitude {Caster} {PerkType.ForceLeap} 10",
            $"Magnitude {Caster} {PerkType.ForceLeap} 10");
    }

    [Test]
    public void Combat_readiness_scales_a_positive_amount_and_an_activated_ability_only_while_tracked()
    {
        _readiness = 50;

        _service.ApplyCombatReadinessMagnitude(Caster, 0).Should().Be(0);
        _service.ApplyCombatReadinessMagnitude(Caster, 9).Should().Be(14, "half again, rounded up");
        _service.ApplyCombatReadinessToActivatedAbilityMagnitude(Caster, 9).Should().Be(9, "no impact is tracked");
        Track(SkillType.Force);
        _service.ApplyCombatReadinessToActivatedAbilityMagnitude(Caster, 9).Should().Be(14);
        _readiness = 0;
        _service.ApplyCombatReadinessMagnitude(Caster, 9).Should().Be(9);
        _log.Should().Equal($"Readiness {Caster}", $"Readiness {Caster}", $"Readiness {Caster}");
    }
}
