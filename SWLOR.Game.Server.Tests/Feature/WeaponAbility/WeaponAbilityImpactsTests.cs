using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>
/// The hostile impact of a weapon ability with fake services: the direct multi-hit path and the telegraphed area path. The ability service
/// stand-in runs the callbacks the impact hands it for one creature, so the closures built here run and their calls reach the transcript.
/// The caps and the creature-state checks inside the damage modifiers and riders have their own tests.
/// </summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityImpactsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityImpacts _part = null!;
    private int _damagePerHit;
    private int _directCalls;
    private int _telegraphedCalls;
    private string? _lastShape;
    private bool? _lastCenterOnActivator;
    private Action<AbilityImpactSummary>? _delayedAfterImpact;
    private readonly List<bool> _animations = new();

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityImpacts>();
        _damagePerHit = 10;
        _directCalls = 0;
        _telegraphedCalls = 0;
        _lastShape = null;
        _lastCenterOnActivator = null;
        _delayedAfterImpact = null;
        _animations.Clear();
        _bench.Leaf<IAbilityService>()
            .Script(nameof(IAbilityService.ApplyCombatImpact), args =>
            {
                _directCalls++;
                _animations.Add((bool)Argument(nameof(IAbilityService.ApplyCombatImpact), args, "playImpactAnimation")!);
                RunCallbacks(nameof(IAbilityService.ApplyCombatImpact), args);
                return _damagePerHit;
            })
            .Script(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args =>
            {
                _telegraphedCalls++;
                _lastShape = Argument(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args, "shape")?.ToString();
                _lastCenterOnActivator = (bool?)Argument(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args, "centerOnActivator");
                RunCallbacks(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args);
                var afterImpact = (Action<AbilityImpactSummary>?)Argument(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args, "afterImpactAction");
                var delayed = (float)Argument(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args, "telegraphDuration")! > 0f;
                if (delayed)
                {
                    // A delayed telegraph resolves later: nothing has landed yet and the callback runs with the summary found then.
                    _delayedAfterImpact = afterImpact;
                    return 0;
                }

                afterImpact?.Invoke(new AbilityImpactSummary { ImpactedTargetCount = 1 });
                return _damagePerHit;
            });
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private static object? Argument(string method, object?[] args, string name)
    {
        var parameters = typeof(IAbilityService).GetMethod(method)!.GetParameters();
        var index = Array.FindIndex(parameters, parameter => parameter.Name == name);
        return index < 0 ? null : args[index];
    }

    private static void RunCallbacks(string method, object?[] args)
    {
        foreach (var name in new[] { "beforeImpact", "baseDamageAdjustment", "damagePercentAdjustment", "beforeSuccessfulImpactRiders", "afterSuccessfulHit" })
        {
            switch (Argument(method, args, name))
            {
                case Action<uint> action:
                    action(Defender);
                    break;
                case Func<uint, int> adjustment:
                    adjustment(Defender);
                    break;
                case Func<uint, StatusEffectCategory?> riders:
                    riders(Defender);
                    break;
            }
        }
    }

    private static WeaponAbilityImpactRequest Request(
        WeaponAbilityProfile profile,
        bool isArea = false,
        AbilityTargetingShapeType shape = AbilityTargetingShapeType.None,
        AbilityTargetingFlags flags = AbilityTargetingFlags.None) =>
        new(profile, SkillType.Invalid, 20, 6, typeof(DisorientedStatusEffect), null!, isArea, shape, 8f, 2f, flags, VisualEffect.None, AbilityType.Invalid, Animation.Invalid);

    private string Impact(WeaponAbilityImpactRequest request) => _bench.Run(() => _part.ApplyHostileImpact(request, Attacker, Defender, default));

    [Test]
    public void ADirectImpactMakesOneCombatImpactPerHit()
    {
        Impact(Request(new WeaponAbilityProfile { HitCount = 3 }));

        _directCalls.Should().Be(3);
        _telegraphedCalls.Should().Be(0);
    }

    [Test]
    public void ADirectImpactMakesAtLeastOneHitWhateverTheHitCount()
    {
        Impact(Request(new WeaponAbilityProfile { HitCount = 0 }));

        _directCalls.Should().Be(1);
    }

    [Test]
    public void OnlyTheFirstHitPlaysTheImpactAnimation()
    {
        Impact(Request(new WeaponAbilityProfile { HitCount = 2 }));

        _animations.Should().Equal(true, false);
    }

    [Test]
    public void ASpentHitPointCostAndTheHostileSelfEffectsComeBeforeTheFirstHit()
    {
        _bench.Leaf<IAbilityImpactState>();
        var profile = new WeaponAbilityProfile { HPCostPercent = 10, SelfHastePercent = 10, SelfHasteDurationSeconds = 6, ApplySelfModifiersOnHostileActivation = true };

        // The engine fake gives the activator 100 hit points, so the cost is 10.
        var text = Impact(Request(profile));

        var firstHit = text.IndexOf("Ability.ApplyCombatImpact(", StringComparison.Ordinal);
        text.IndexOf("ApplyHitPointSpendAbilityEffects(Attacker, 10", StringComparison.Ordinal)
            .Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(firstHit);
        text.IndexOf("AddCapped(Attacker, AttackDelayReductionPercent, 10, 6", StringComparison.Ordinal)
            .Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(firstHit);
    }

    [Test]
    public void TheDefenseIgnoreOfTheHitIsRecordedForTheActivationBeforeTheImpact()
    {
        var text = Impact(Request(new WeaponAbilityProfile { DefenseIgnorePercent = 25 }));

        text.IndexOf("AddActiveAbilityDefenseIgnorePercentAdjustment(Attacker, 25", StringComparison.Ordinal)
            .Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(text.IndexOf("Ability.ApplyCombatImpact(", StringComparison.Ordinal));
    }

    [Test]
    public void EveryHitRunsTheRidersThroughTheCallbacksTheAbilityServiceHandsBack()
    {
        var profile = new WeaponAbilityProfile { RestoreStaminaOnHit = 3, DrainFPOnHit = 4 };

        var text = Impact(Request(profile));

        text.Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("ReduceFP(Defender, 4")
            .And.Contain("ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender");
    }

    [Test]
    public void ARestoreByTargetCountReadsTheImpactSummaryAfterTheLastHit()
    {
        // Each hit adds one impacted target, so the summary reaches the minimum only after the third and last hit.
        _bench.Leaf<IAbilityImpactState>().Script(nameof(IActiveAbilityImpact.GetActiveAbilityImpactSummary),
            _ => new AbilityImpactSummary { ImpactedTargetCount = _directCalls });
        var profile = new WeaponAbilityProfile { HitCount = 3, RestoreStaminaIfMinimumTargetsHit = 9, StaminaRestoreMinimumTargets = 3 };

        var text = Impact(Request(profile));

        text.Split("Ability.ApplyCombatImpact(").Length.Should().Be(4);
        text.IndexOf("RestoreAbilityHitStamina(Attacker", StringComparison.Ordinal)
            .Should().BeGreaterThan(text.LastIndexOf("Ability.ApplyCombatImpact(", StringComparison.Ordinal));
    }

    [Test]
    public void ADirectImpactReportsTheHitsThatDealtDamageToTheAfterImpactEffects()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 2);
        var profile = new WeaponAbilityProfile { HitCount = 3, RestoreFPIfAllHitsLand = 2 };

        Impact(Request(profile)).Should().Contain("RestoreFP(Attacker, 2");

        _damagePerHit = 0;
        Impact(Request(profile)).Should().NotContain("RestoreFP(Attacker, 2");
    }

    [Test]
    public void ASpreadPrerequisiteCaptureIsHandedToTheImpactOnlyForAProfileThatSpreads()
    {
        var spreading = new WeaponAbilityProfile { SpreadBleedFromTarget = true };

        Impact(Request(spreading)).Should().Contain("HasStatusEffectCategory");
        Impact(Request(new WeaponAbilityProfile())).Should().NotContain("HasStatusEffectCategory");
    }

    [Test]
    public void AnAreaAbilityWithAShapeTakesTheTelegraphedPathOnce()
    {
        var text = Impact(Request(new WeaponAbilityProfile { HitCount = 3, TelegraphDuration = 1.5f }, isArea: true, shape: AbilityTargetingShapeType.Cone));

        _telegraphedCalls.Should().Be(1);
        _directCalls.Should().Be(0);
        _lastShape.Should().Be("Cone");
        text.Should().Contain("ClearAbilityActivationIdleBonuses(Attacker");
    }

    [Test]
    public void TheShapeOfTheAbilityBecomesTheMatchingImpactShape()
    {
        Impact(Request(new WeaponAbilityProfile(), true, AbilityTargetingShapeType.Rect));
        _lastShape.Should().Be("Line");

        Impact(Request(new WeaponAbilityProfile(), true, AbilityTargetingShapeType.Sphere));
        _lastShape.Should().Be("Sphere");
    }

    [Test]
    public void AnAreaAbilityWithoutAShapeOrATargetCapStaysOnTheDirectPath()
    {
        Impact(Request(new WeaponAbilityProfile(), isArea: true));

        _telegraphedCalls.Should().Be(0);
        _directCalls.Should().Be(1);
    }

    [Test]
    public void ATargetCapMakesAnAreaAbilityTelegraphedEvenWithoutAShape()
    {
        Impact(Request(new WeaponAbilityProfile { MaximumAreaTargets = 3 }, isArea: true));

        _telegraphedCalls.Should().Be(1);
    }

    [Test]
    public void ANonAreaAbilityNeverTakesTheTelegraphedPath()
    {
        Impact(Request(new WeaponAbilityProfile { MaximumAreaTargets = 3 }, shape: AbilityTargetingShapeType.Cone));

        _telegraphedCalls.Should().Be(0);
    }

    [Test]
    public void ATelegraphedImpactAppliesTheAfterActivationModifiersOnlyWhenItHasADelayAndStruckATarget()
    {
        var delayed = new WeaponAbilityProfile { TelegraphDuration = 1.5f, SelfHastePercent = 10, SelfHasteDurationSeconds = 6 };
        var instant = new WeaponAbilityProfile { SelfHastePercent = 10, SelfHasteDurationSeconds = 6 };

        const string haste = "AddCapped(Attacker, AttackDelayReductionPercent, 10, 6";
        // The instant path applies the landed modifiers once after the impact. The delayed path lands nothing yet, so it applies them
        // only when the telegraph resolves and its callback reports a struck target.
        Impact(Request(instant, true, AbilityTargetingShapeType.Cone)).Split(haste).Length.Should().Be(2);

        Impact(Request(delayed, true, AbilityTargetingShapeType.Cone)).Should().NotContain(haste);
        _delayedAfterImpact.Should().NotBeNull();

        _bench.Run(() => _delayedAfterImpact!(new AbilityImpactSummary { ImpactedTargetCount = 0 })).Should().NotContain(haste);
        _bench.Run(() => _delayedAfterImpact!(new AbilityImpactSummary { ImpactedTargetCount = 1 })).Split(haste).Length.Should().Be(2);
    }

    [Test]
    public void ADelayedTelegraphLandsNothingWhenItIsScheduled()
    {
        var text = Impact(Request(new WeaponAbilityProfile { TelegraphDuration = 1.5f, SelfHastePercent = 10, SelfHasteDurationSeconds = 6 }, true, AbilityTargetingShapeType.Cone));

        text.Should().NotContain("AddCapped(Attacker, AttackDelayReductionPercent");
        _delayedAfterImpact.Should().NotBeNull();
    }

    [Test]
    public void ATelegraphedImpactClearsTheIdleSnapshotsEvenWhenTheImpactThrows()
    {
        // The engine's native library is not loaded in a unit test: the bench ends the step there, after the part's finally block ran.
        _bench.Leaf<IAbilityService>().Script(nameof(IAbilityService.ApplyTelegraphedCombatImpact), _ => throw new DllNotFoundException("native"));

        var text = Impact(Request(new WeaponAbilityProfile(), true, AbilityTargetingShapeType.Cone));

        text.Should().Contain("stopped at the native boundary").And.Contain("ClearAbilityActivationIdleBonuses(Attacker");
    }

    [Test]
    public void ACenteredAreaAbilityIsCenteredOnTheActivator()
    {
        Impact(Request(new WeaponAbilityProfile(), true, AbilityTargetingShapeType.Cone, AbilityTargetingFlags.OriginOnSelf));
        _lastCenterOnActivator.Should().BeTrue();

        Impact(Request(new WeaponAbilityProfile(), true, AbilityTargetingShapeType.Cone, AbilityTargetingFlags.None));
        _lastCenterOnActivator.Should().BeFalse();
    }

    [Test]
    public void TheStatusCategoriesOfAHostileAbilityComeFromTheImpactStatusAndNothingForAFriendlyOne()
    {
        _bench.Leaf<IAbilityStatusApplicationService>()
            .Script(nameof(IAbilityStatusApplicationService.GetCombatImpactStatusCategories), _ => StatusEffectCategory.Bleeding);
        var profile = new WeaponAbilityProfile();
        StatusEffectCategory hostile = default;
        StatusEffectCategory friendly = StatusEffectCategory.Bleeding;

        _bench.Run(() => hostile = _part.ResolveStatusCategories(profile, Attacker, true, typeof(DisorientedStatusEffect), 5, null!));
        var text = _bench.Run(() => friendly = _part.ResolveStatusCategories(profile, Attacker, false, typeof(DisorientedStatusEffect), 5, null!));

        hostile.Should().Be(StatusEffectCategory.Bleeding);
        friendly.Should().Be(StatusEffectCategory.None);
        text.Should().NotContain("GetCombatImpactStatusCategories");
    }
}
