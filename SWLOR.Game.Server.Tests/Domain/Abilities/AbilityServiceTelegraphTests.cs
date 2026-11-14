using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>Area impacts: the footprint, the instant flash and the telegraph that resolves later.</summary>
[TestFixture]
public class AbilityServiceTelegraphTests : AbilityServiceTestBase
{
    private int Instant(
        CombatImpactAreaShape shape = CombatImpactAreaShape.Sphere,
        float length = 5f,
        float width = 0f,
        int maxTargets = 0,
        bool alwaysVisual = true,
        bool sendsNoTarget = true,
        bool centerOnActivator = false,
        float flash = AbilityServiceFlash,
        Action<TelegraphGeometry>? resolved = null,
        Action<AbilityImpactSummary>? after = null) =>
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, shape, 0f, length, width,
            centerOnActivator: centerOnActivator, areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst, afterImpactAction: after!,
            maxTargets: maxTargets, alwaysApplyAreaVisualEffect: alwaysVisual, sendsNoTargetMessage: sendsNoTarget,
            impactFlashDuration: flash, onGeometryResolved: resolved!, playImpactAnimation: false);

    private const float AbilityServiceFlash = 0.3f;

    /// <summary>Where the first log entry that starts with each prefix sits; every prefix must be there.</summary>
    private List<int> OrderOf(params string[] prefixes) =>
        prefixes.Select(prefix =>
        {
            var index = Bench.Log.FindIndex(entry => entry.StartsWith(prefix, StringComparison.Ordinal));
            index.Should().BeGreaterThanOrEqualTo(0, prefix);
            return index;
        }).ToList();

    [Test]
    public void An_instant_area_impact_resolves_its_footprint_flashes_then_hits_what_is_inside()
    {
        Bench.CreaturesInArea.AddRange(new[] { Target, Caster });
        Service.BeginAbilityImpact(Caster, Hostile());
        Bench.Log.Clear();
        TelegraphGeometry? geometry = null;

        var dealt = Instant(resolved: resolved => geometry = resolved);

        dealt.Should().BeGreaterThan(0);
        geometry!.Value.Shape.Should().Be(TelegraphType.Sphere);
        geometry.Value.Position.Should().Be(Bench.Positions[Target]);
        OrderOf("Flash:", "EffectAt:", "BattleTheme:").Should().BeInAscendingOrder();
        Service.GetActiveAbilityImpactSummary(Caster).IsAreaAbility.Should().BeTrue();
    }

    [Test]
    public void The_after_impact_action_receives_the_summary_of_an_instant_impact_only_when_tracked()
    {
        var summaries = new List<AbilityImpactSummary>();

        Instant(after: summaries.Add);
        summaries.Should().BeEmpty();

        Service.BeginAbilityImpact(Caster, Hostile());
        Instant(after: summaries.Add);
        summaries.Should().ContainSingle();
    }

    [TestCase(CombatImpactAreaShape.Sphere, "Sphere")]
    [TestCase(CombatImpactAreaShape.Cone, "Cone")]
    [TestCase(CombatImpactAreaShape.Line, "Line")]
    public void Each_shape_has_its_own_telegraph(CombatImpactAreaShape shape, string name)
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Bench.Log.Clear();

        var dealt = Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, shape, 2f, 6f, 3f, playImpactAnimation: false);

        dealt.Should().Be(0, "the damage waits for the telegraph");
        Bench.PendingTelegraph.Should().NotBeNull();
        OrderOf($"{name}Telegraph:{Caster}", $"DeferStamina:{Caster}:Blast").Should().BeInAscendingOrder();
    }

    [Test]
    public void An_unknown_shape_has_no_telegraph()
    {
        var run = () => Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, (CombatImpactAreaShape)99, 2f, 6f, playImpactAnimation: false);

        run.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void The_telegraph_resolves_its_own_tracked_impact_with_the_bonuses_it_captured()
    {
        var ability = Hostile();
        ability.DealsDeferredDamage = true;
        Service.NextAbilityDamageBonus = 3;
        Service.StatusAppliedBonus = 0;
        Service.BeginAbilityImpact(Caster, ability);
        var summaries = new List<AbilityImpactSummary>();
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst, afterImpactAction: summaries.Add, playImpactAnimation: false);
        Service.EndAbilityImpact(Caster);
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Target, Other });

        summaries.Should().ContainSingle().Which.ImpactedTargetCount.Should().Be(2);
        Bench.Log.Should().ContainInOrder(
            "GetStatusAppliedBonus",
            $"EffectAt:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}",
            $"BattleTheme:{Target}:{Caster}",
            $"BattleTheme:{Other}:{Caster}",
            $"ApplyAbilityImpactEffects:{Caster}:2",
            $"CompleteDeferredStamina:{Caster}:Blast");
        LogOf("NotifyAttackAttempt").Should().BeEmpty("the telegraph's impact is not another attack attempt");
    }

    [Test]
    public void A_telegraph_with_no_hostile_left_reports_it_and_keeps_the_stamina_cost_paid()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst, playImpactAnimation: false);
        Bench.Hostile.Clear();
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Target });

        Bench.Log.Should().Equal(
            $"EffectAt:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}",
            $"Nearby:{Caster}:Blast:no target:60",
            $"CompleteDeferredStamina:{Caster}:Blast");
    }

    [Test]
    public void A_telegraph_can_stay_quiet_about_no_targets()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst, alwaysApplyAreaVisualEffect: false, sendsNoTargetMessage: false,
            playImpactAnimation: false);
        Bench.Hostile.Clear();
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Target });

        Bench.Log.Should().Equal($"CompleteDeferredStamina:{Caster}:Blast");
    }

    [Test]
    public void A_dead_creator_resolves_nothing_but_still_completes_the_stamina_context()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f, playImpactAnimation: false);
        Bench.Hp[Caster] = 0;
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Target });

        Bench.Log.Should().Equal($"CompleteDeferredStamina:{Caster}:Blast");
    }

    [Test]
    public void A_control_telegraph_consumes_the_armed_bonuses_only_when_its_target_turns_out_to_take_damage()
    {
        Service.NextAbilityDamageBonus = 3;
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 0, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            baseDamageAdjustment: target => target == Other ? 10 : 0, playImpactAnimation: false);
        LogOf("ConsumeNextAbilityDamageBonus").Should().BeEmpty("the telegraph has not resolved");
        Service.EndAbilityImpact(Caster);

        Bench.PendingTelegraph!(Caster, new List<uint> { Target, Other });

        LogOf("ConsumeNextAbilityDamageBonus").Should().ContainSingle();
    }

    [Test]
    public void A_telegraph_with_a_limit_takes_the_closest_to_its_origin()
    {
        Bench.Positions[Target] = new Vector3(1f, 0f, 0f);
        Bench.Positions[Other] = new Vector3(4f, 0f, 0f);
        Service.BeginAbilityImpact(Caster, Hostile());
        var ability = Hostile();
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            maxTargets: 1, playImpactAnimation: false);
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Other, Target });

        LogOf("ModifyEnmity").Should().Equal($"ModifyEnmity:{Caster}:{Target}:100");
    }

    [Test]
    public void A_failure_inside_the_telegraph_aborts_its_impact_and_completes_the_stamina_context()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f,
            afterSuccessfulHit: _ => throw new InvalidOperationException("boom"), playImpactAnimation: false);
        Service.EndAbilityImpact(Caster);
        Bench.Log.Clear();

        var run = () => Bench.PendingTelegraph!(Caster, new List<uint> { Target });

        run.Should().Throw<InvalidOperationException>();
        Service.GetActiveAbilityImpactSummary(Caster).Should().BeNull();
        Bench.Log.Should().Contain($"CompleteDeferredStamina:{Caster}:Blast");
    }

    [Test]
    public void A_telegraph_with_no_tracked_ability_runs_the_hits_without_tracking_them()
    {
        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, CombatImpactAreaShape.Sphere, 2f, 6f, playImpactAnimation: false);
        Bench.Log.Clear();

        Bench.PendingTelegraph!(Caster, new List<uint> { Target });

        Bench.Log.Should().Contain($"BattleTheme:{Target}:{Caster}");
        Bench.Log.Should().Contain($"CompleteDeferredStamina:{Caster}:");
        Service.GetActiveAbilityImpactSummary(Caster).Should().BeNull();
    }

    [TestCase(0f, true)]
    [TestCase(0.3f, false)]
    public void The_flash_needs_a_duration_and_a_size_and_the_telegraph_service_agrees(float duration, bool hidden)
    {
        Service.BeginAbilityImpact(Caster, Hostile());

        Instant(flash: duration);

        LogOf("Flash").Should().HaveCount(hidden ? 0 : 1);
    }

    [Test]
    public void The_flash_is_suppressed_when_the_activation_marker_already_shows_the_shape()
    {
        Bench.ImpactFlashShows = false;
        Service.BeginAbilityImpact(Caster, Hostile());

        Instant();

        LogOf("Flash").Should().BeEmpty();
    }

    [Test]
    public void A_shape_the_telegraph_resolves_to_nothing_has_no_flash()
    {
        Bench.ResolveGeometry = impact => impact with { Size = Vector2.Zero };
        Service.BeginAbilityImpact(Caster, Hostile());

        Instant();

        LogOf("Flash").Should().BeEmpty();
    }

    [TestCase(CombatImpactAreaShape.Cone, 8f, 8f, true, false)]
    [TestCase(CombatImpactAreaShape.Line, 8f, 2f, true, false)]
    public void Directional_shapes_aim_from_the_caster_and_only_hit_what_is_inside(CombatImpactAreaShape shape, float length, float width, bool inside, bool behind)
    {
        Bench.CreaturesInArea.AddRange(new[] { Target, Other });
        Bench.Positions[Other] = new Vector3(-4f, 0f, 0f);
        Service.BeginAbilityImpact(Caster, Hostile());
        Bench.Log.Clear();
        TelegraphGeometry? geometry = null;

        Instant(shape, length, width, resolved: resolved => geometry = resolved);

        geometry!.Value.Position.Should().Be(Vector3.Zero);
        geometry.Value.Rotation.Should().Be(0f);
        LogOf("ModifyEnmity").Should().Equal($"ModifyEnmity:{Caster}:{Target}:100");
    }

    [Test]
    public void A_target_on_top_of_the_caster_aims_along_the_casters_facing()
    {
        Bench.Positions[Target] = Vector3.Zero;
        Bench.Facing = 90f;
        Service.BeginAbilityImpact(Caster, Hostile());
        TelegraphGeometry? geometry = null;

        Instant(CombatImpactAreaShape.Cone, resolved: resolved => geometry = resolved);

        geometry!.Value.Rotation.Should().BeApproximately((float)(Math.PI / 2d), 0.0001f);
    }

    [Test]
    public void A_location_aims_the_shape_and_with_nothing_to_aim_at_the_caster_stands_at_the_origin()
    {
        Bench.Invalid.Add(Target);
        Service.BeginAbilityImpact(Caster, Hostile());
        TelegraphGeometry? geometry = null;

        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.At(AbilityBench.TheArea, new Vector3(0f, 5f, 0f)), SkillType.Force, 10, 0, null!,
            CombatImpactAreaShape.Sphere, 0f, 6f, onGeometryResolved: resolved => geometry = resolved, playImpactAnimation: false);
        geometry!.Value.Position.Should().Be(new Vector3(0f, 5f, 0f));

        Service.ApplyTelegraphedCombatImpact(
            Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!,
            CombatImpactAreaShape.Sphere, 0f, 6f, onGeometryResolved: resolved => geometry = resolved, playImpactAnimation: false);
        geometry!.Value.Position.Should().Be(Vector3.Zero);
    }

    [Test]
    public void A_centered_sphere_stays_on_the_caster()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        TelegraphGeometry? geometry = null;

        Instant(centerOnActivator: true, resolved: resolved => geometry = resolved);

        geometry!.Value.Position.Should().Be(Vector3.Zero);
    }

    [Test]
    public void A_back_offset_origin_moves_the_shape_behind_the_caster_and_lengthens_it()
    {
        var ability = Hostile();
        ability.Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Rect, 8f, 2f, AbilityTargetingFlags.BackOffsetOrigin, null!);
        Service.BeginAbilityImpact(Caster, ability);
        TelegraphGeometry? geometry = null;

        Instant(CombatImpactAreaShape.Line, 8f, 2f, resolved: resolved => geometry = resolved);

        geometry!.Value.Position.X.Should().BeLessThan(0f);
        geometry.Value.Size.X.Should().BeGreaterThan(8f);
    }

    [Test]
    public void The_hostiles_in_a_sphere_are_the_living_hostiles_in_sight_nearest_first()
    {
        Bench.CreaturesInArea.AddRange(new[] { Other, Target, Caster, 600u, 601u, 602u });
        Bench.Positions[600] = new Vector3(1f, 0f, 0f);
        Bench.Positions[601] = new Vector3(1f, 0f, 0f);
        Bench.Positions[602] = new Vector3(100f, 0f, 0f);
        Bench.Hostile.Add((600, Caster));
        Bench.Hostile.Add((601, Caster));
        Bench.Hostile.Add((602, Caster));
        Bench.Dead.Add(600);
        Bench.Hp[601] = 0;
        Bench.NoLineOfSight.Add(Other);
        Bench.Positions[Other] = new Vector3(2f, 0f, 0f);

        var found = Service.GetHostileTargetsInSphere(Caster, Bench.At(AbilityBench.TheArea, Vector3.Zero), 5f);

        found.Should().Equal(Target);
    }

    [Test]
    public void The_hostiles_of_an_area_with_no_valid_area_are_none()
    {
        Service.GetHostileTargetsInSphere(Caster, Bench.Nowhere(), 5f).Should().BeEmpty();
    }

    [TestCase(CombatImpactAreaShape.Sphere, 3f, 0f, 5f, 0f, true)]
    [TestCase(CombatImpactAreaShape.Sphere, 6f, 0f, 5f, 0f, false)]
    [TestCase(CombatImpactAreaShape.Cone, 0f, 0f, 5f, 0f, true)]
    [TestCase(CombatImpactAreaShape.Cone, 3f, 0f, 5f, 0f, true)]
    [TestCase(CombatImpactAreaShape.Cone, 0f, 3f, 5f, 2f, false)]
    [TestCase(CombatImpactAreaShape.Line, 3f, 0f, 5f, 0f, true)]
    [TestCase(CombatImpactAreaShape.Line, 3f, 2f, 5f, 2f, false)]
    [TestCase(CombatImpactAreaShape.Line, -1f, 0f, 5f, 2f, false)]
    public void The_shape_test_matches_the_footprint(CombatImpactAreaShape shape, float x, float y, float length, float width, bool expected)
    {
        var inside = (bool)AbilityInternals.Method("IsPositionInCombatImpactShape").Invoke(
            Service, new object[] { new Vector3(x, y, 0f), Vector3.Zero, 0f, shape, length, width })!;

        inside.Should().Be(expected);
    }

    [Test]
    public void An_unknown_shape_is_never_inside_or_searched()
    {
        var inside = () => AbilityInternals.Method("IsPositionInCombatImpactShape").Invoke(
            Service, new object[] { Vector3.Zero, Vector3.Zero, 0f, (CombatImpactAreaShape)99, 5f, 0f });
        var search = () => AbilityInternals.Method("GetCombatImpactShapeSearchRadius").Invoke(
            Service, new object[] { (CombatImpactAreaShape)99, 5f, 0f });

        inside.Should().Throw<System.Reflection.TargetInvocationException>().WithInnerException<ArgumentOutOfRangeException>();
        search.Should().Throw<System.Reflection.TargetInvocationException>().WithInnerException<ArgumentOutOfRangeException>();
    }

    [Test]
    public void An_unknown_geometry_is_not_searched()
    {
        Bench.ResolveGeometry = impact => impact with { Shape = TelegraphType.None };
        Service.BeginAbilityImpact(Caster, Hostile());

        var run = () => Instant();

        run.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void The_area_search_radius_of_a_line_reaches_its_far_corners()
    {
        var radius = (float)AbilityInternals.Method("GetCombatImpactShapeSearchRadius").Invoke(
            Service, new object[] { CombatImpactAreaShape.Line, 3f, 8f })!;

        radius.Should().Be(5f);
    }
}
