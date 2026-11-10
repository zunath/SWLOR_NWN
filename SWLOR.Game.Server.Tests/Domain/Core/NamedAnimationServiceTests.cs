using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the named animation service: the engine is a recording fake whose closures (AssignCommand,
/// ActionDoCommand, DelayCommand) the tests run by hand, in the order the engine would, so the mappings, the carrier
/// animation, the queue steps and the timeouts can be asserted. The ownership rules themselves are covered by
/// <c>NamedAnimationPlaybackTests</c> and <c>EquipmentAnimationPlaybackTests</c>.
/// </summary>
[TestFixture]
public class NamedAnimationServiceTests
{
    private const uint Creature = 10;
    private const uint Module = 1;
    private const uint Rifle = 20;
    private const uint Invalid = 0x7F000000;
    private const string TokenVariable = "_NAMED_ANIMATION_PLAYBACK";

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private LocalVariables _locals = null!;
    private NamedAnimationService _service = null!;
    private readonly HashSet<object> _ran = new(ReferenceEqualityComparer.Instance);
    private bool _rifleInHand;
    private BaseItem _weaponType;

    [SetUp]
    public void SetUp()
    {
        _ran.Clear();
        _rifleInHand = false;
        _weaponType = BaseItem.Rifle;
        _engine = new FakeNWScript();
        _scope = NWScriptRuntime.Override(_engine);
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        _engine.SetReturn("GetObjectType", ObjectType.Creature);
        _engine.SetReturn("GetModule", Module);
        _engine.SetReturn("OBJECT_SELF", Creature);
        _engine.SetReturn("GetCurrentAction", ActionType.Invalid);
        _engine.SetReturn("GetCurrentHitPoints", 10);
        _engine.SetReturnHandler("GetItemInSlot", arguments =>
            _rifleInHand && (InventorySlot)arguments[0]! == InventorySlot.RightHand ? Rifle : Invalid);
        _engine.SetReturnHandler("GetBaseItemType", _ => _weaponType);
        _service = new NamedAnimationService();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    // Runs the closures of the calls to a member that have not run yet, oldest first, once. Closures that the run
    // itself records wait for the next call.
    private void RunNew(string member, Func<IReadOnlyList<object?>, bool>? where = null, int limit = int.MaxValue)
    {
        foreach (var call in _engine.CallsTo(member))
        {
            var action = (Action)call.Arguments[^1]!;
            if (limit > 0 && (where?.Invoke(call.Arguments) ?? true) && _ran.Add(action))
            {
                limit--;
                action();
            }
        }
    }

    private void RunAssigned(uint target) => RunNew("AssignCommand", arguments => (uint)arguments[0]! == target);

    private void RunDeferred(int count = int.MaxValue) => RunNew("ActionDoCommand", limit: count);

    // The engine's timers: a scheduled callback is an AssignCommand to the module that delays the callback.
    private void RunTimers(float seconds)
    {
        RunAssigned(Module);
        RunNew("DelayCommand", arguments => Math.Abs((float)arguments[0]! - seconds) < 0.0001f);
    }

    private string Token() => _locals.String(Creature, TokenVariable);

    private Dictionary<string, string> Mappings() => _engine.CallsTo("ReplaceObjectAnimation")
        .Where(call => (uint)call.Arguments[0]! == Creature)
        .GroupBy(call => (string)call.Arguments[1]!)
        .ToDictionary(group => group.Key, group => (string)group.Last().Arguments[2]!);

    private (Animation Animation, float Speed, float Seconds)[] Played() => _engine.CallsTo("PlayAnimation")
        .Select(call => ((Animation)call.Arguments[0]!, (float)call.Arguments[1]!, (float)call.Arguments[2]!)).ToArray();

    private static AnimationClip Clip(float duration = 2f) => new("sw_wave", duration);

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        _engine.ClearCalls();

        _ = new NamedAnimationService();

        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void Play_maps_the_clip_and_plays_the_pointing_carrier_when_the_closure_runs()
    {
        var token = _service.Play(Creature, Clip());

        token.Should().NotBeNullOrEmpty();
        Token().Should().Be(token);
        var mappings = Mappings();
        mappings["custom1start"].Should().Be("sw_wave_in");
        mappings["custom1lp"].Should().Be("sw_wave");
        mappings["custom1end"].Should().Be("sw_wave_out");
        mappings["dodges"].Should().BeEmpty();
        mappings["xbowr"].Should().BeEmpty();
        mappings["plpause1"].Should().BeEmpty();
        Played().Should().BeEmpty("the animation is a deferred closure");

        RunAssigned(Creature);

        Played().Should().Equal((Animation.PointForward, 1f, 2f));
    }

    [Test]
    public void Play_uses_the_given_duration_and_fits_the_clip_speed_to_it_on_request()
    {
        _service.Play(Creature, Clip(4f), 2f, fitToDuration: true);
        RunAssigned(Creature);

        Played().Should().Equal((Animation.PointForward, 2f, 2f));
    }

    [Test]
    public void Play_with_a_rifle_in_hand_uses_the_dodge_carrier_and_the_hold_mappings()
    {
        _rifleInHand = true;

        _service.Play(Creature, Clip(4f), 2f);
        RunAssigned(Creature);

        var mappings = Mappings();
        mappings["dodges"].Should().Be("sw_wave");
        mappings["xbowr"].Should().Be(NamedAnimationPlayback.NoHoldName);
        Played().Should().Equal((Animation.FireForgetDodgeSide, 2f, 2f));
    }

    [Test]
    public void Play_refuses_equipment_that_does_not_match_the_requirement()
    {
        var token = _service.Play(Creature, Clip(), equipmentRequirement: AnimationEquipmentRequirement.Rifle);

        token.Should().BeNull();
        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty();
        Token().Should().BeEmpty();
    }

    [Test]
    public void Play_accepts_equipment_that_matches_the_requirement()
    {
        var token = _service.Play(Creature, Clip(), equipmentRequirement: AnimationEquipmentRequirement.Unarmed);

        token.Should().NotBeNullOrEmpty();
        Token().Should().Be(token);
    }

    [Test]
    public void Play_checks_the_clip_and_the_duration()
    {
        Action noClip = () => _service.Play(Creature, null!);
        noClip.Should().Throw<ArgumentNullException>();
        foreach (var duration in new[] { 0f, -1f, 601f, float.NaN, float.PositiveInfinity })
        {
            Action invalid = () => _service.Play(Creature, Clip(), duration);
            invalid.Should().Throw<ArgumentOutOfRangeException>($"{duration} seconds is not a duration");
        }

        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty();
    }

    [Test]
    public void A_pose_ends_at_its_duration_and_the_mappings_are_restored_after_the_grace_period()
    {
        var token = _service.Play(Creature, Clip());
        RunAssigned(Creature);

        RunTimers(2f);
        Token().Should().Be("ending:" + token, "the authored exit stays mapped while the engine leaves the emote");
        Mappings()["custom1lp"].Should().Be("sw_wave");

        RunTimers(NamedAnimationPlayback.ExitGraceSeconds);
        Token().Should().BeEmpty();
        Mappings().Values.Should().OnlyContain(value => value == string.Empty);
    }

    [Test]
    public void A_pose_that_nothing_completes_is_ended_by_the_module_timeout()
    {
        var token = _service.Play(Creature, Clip());

        RunTimers(3f);

        Token().Should().Be("ending:" + token);
    }

    [Test]
    public void A_finished_pose_returns_to_the_idle_pause_only_when_the_creature_is_free()
    {
        _service.Play(Creature, Clip());
        RunAssigned(Creature);

        RunTimers(2f);
        RunAssigned(Creature);

        Played().Should().Equal((Animation.PointForward, 1f, 2f), (Animation.LoopingPause, 1f, .1f));
    }

    [TestCase("hit points")]
    [TestCase("combat")]
    [TestCase("action")]
    public void A_finished_pose_stays_put_when_the_creature_died_fights_or_acts(string reason)
    {
        _service.Play(Creature, Clip());
        RunAssigned(Creature);
        switch (reason)
        {
            case "hit points": _engine.SetReturn("GetCurrentHitPoints", 0); break;
            case "combat": _engine.SetReturn("GetIsInCombat", true); break;
            default: _engine.SetReturn("GetCurrentAction", ActionType.MoveToPoint); break;
        }

        RunTimers(2f);
        RunAssigned(Creature);

        Played().Should().Equal((Animation.PointForward, 1f, 2f));
    }

    [Test]
    public void A_pose_a_newer_playback_replaced_does_not_play_its_late_closure()
    {
        _service.Play(Creature, Clip());
        var second = _service.Play(Creature, new AnimationClip("sw_bow", 1f));

        RunAssigned(Creature);

        Played().Should().Equal((Animation.PointForward, 1f, 1f));
        Token().Should().Be(second);
    }

    [Test]
    public void StopIfCurrent_stops_only_the_playback_it_was_given_the_token_of()
    {
        var first = _service.Play(Creature, Clip());
        var second = _service.Play(Creature, new AnimationClip("sw_bow", 1f));

        _service.StopIfCurrent(Creature, first).Should().BeFalse();
        Token().Should().Be(second);
        _service.StopIfCurrent(Creature, second).Should().BeTrue();
        Token().Should().Be("ending:" + second);
    }

    [Test]
    public void Stop_ends_the_pose_and_can_clear_an_idle_creatures_queue()
    {
        var token = _service.Play(Creature, Clip());

        _service.Stop(Creature, cancelQueuedAnimation: true);

        _engine.CallsTo("ClearAllActions").Should().ContainSingle();
        Token().Should().Be("ending:" + token);
    }

    [Test]
    public void Stop_leaves_a_busy_creatures_queue_alone()
    {
        _service.Play(Creature, Clip());
        _engine.SetReturn("GetCurrentAction", ActionType.AttackObject);

        _service.Stop(Creature, cancelQueuedAnimation: true);

        _engine.CallsTo("ClearAllActions").Should().BeEmpty();
    }

    [Test]
    public void Stop_without_a_pose_does_nothing()
    {
        _service.Stop(Creature, cancelQueuedAnimation: true);

        _engine.CallsTo("ClearAllActions").Should().BeEmpty();
        Token().Should().BeEmpty();
    }

    [Test]
    public void ReleaseForNativePlayback_restores_the_mappings_without_an_idle_pause()
    {
        _service.Play(Creature, Clip());
        RunAssigned(Creature);

        _service.ReleaseForNativePlayback(Creature);

        Token().Should().BeEmpty();
        Mappings().Values.Should().OnlyContain(value => value == string.Empty);
        _engine.CallsTo("AssignCommand").Count(call => (uint)call.Arguments[0]! == Creature)
            .Should().Be(1, "only the playback closure was assigned to the creature");
    }

    [Test]
    public void ClearOnDeath_drops_the_pose_at_once()
    {
        _service.Play(Creature, Clip());

        _service.ClearOnDeath(Creature);

        Token().Should().BeEmpty();
        Mappings().Values.Should().OnlyContain(value => value == string.Empty);
    }

    [Test]
    public void Queue_rejects_a_clip_a_duration_and_a_target_that_cannot_play()
    {
        Action noClip = () => _service.Queue(Creature, null!);
        noClip.Should().Throw<ArgumentNullException>();
        Action badDuration = () => _service.Queue(Creature, Clip(), 0f);
        badDuration.Should().Throw<ArgumentOutOfRangeException>();
        Action gone = () => _service.Queue(Invalid, Clip());
        gone.Should().Throw<ArgumentException>();
        _engine.SetReturn("GetObjectType", ObjectType.Placeable);
        Action notACreature = () => _service.Queue(Creature, Clip());
        notACreature.Should().Throw<ArgumentException>();

        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void Queue_holds_the_queue_for_the_clip_and_completes_it_afterwards()
    {
        _service.Queue(Creature, Clip(), 3f);

        _engine.CallsTo("ActionDoCommand").Should().BeEmpty("nothing is queued until the assigned closure runs");
        RunAssigned(Creature);

        _engine.Calls.Where(call => call.Member is "ActionDoCommand" or "ActionWait").Select(call => call.Member)
            .Should().Equal("ActionDoCommand", "ActionWait", "ActionDoCommand");
        ((float)_engine.CallsTo("ActionWait").Single().Arguments[0]!).Should().Be(3f);
        _engine.CallsTo("ActionPlayAnimation").Should().BeEmpty("the carrier is chosen when the clip starts");

        RunDeferred(1);

        Mappings()["custom1lp"].Should().Be("sw_wave");
        Played().Should().Equal((Animation.PointForward, 1f, 3f));
        var token = Token();
        token.Should().NotBeEmpty().And.NotStartWith("ending:");
        RunDeferred(1);
        Token().Should().Be("ending:" + token, "the queued completion ends the pose");
    }

    [Test]
    public void Queue_chooses_the_carrier_and_the_hold_with_the_equipment_worn_when_the_clip_starts()
    {
        _service.Queue(Creature, Clip(4f), 2f);
        RunAssigned(Creature);
        _rifleInHand = true;

        RunDeferred();

        Mappings()["dodges"].Should().Be("sw_wave");
        Played().Should().Equal((Animation.FireForgetDodgeSide, 2f, 2f));
    }

    [Test]
    public void Queue_with_a_requirement_the_equipment_no_longer_meets_leaves_the_carrier_native()
    {
        _service.Queue(Creature, Clip(), equipmentRequirement: AnimationEquipmentRequirement.Unarmed);
        RunAssigned(Creature);
        _rifleInHand = true;
        _weaponType = BaseItem.Longsword;

        RunDeferred();

        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty();
        Token().Should().BeEmpty();
        Played().Should().Equal((Animation.PointForward, 1f, 2f));
    }

    [Test]
    public void An_equipment_change_before_the_queue_runs_cancels_the_restricted_clip()
    {
        _service.Queue(Creature, Clip(), equipmentRequirement: AnimationEquipmentRequirement.Unarmed);

        _service.OnEquipmentChanging();
        RunAssigned(Creature);

        _engine.CallsTo("ActionDoCommand").Should().BeEmpty();
        _engine.CallsTo("ActionWait").Should().BeEmpty();
    }

    [Test]
    public void An_equipment_change_after_the_queue_ran_leaves_the_carrier_native()
    {
        _service.Queue(Creature, Clip(), equipmentRequirement: AnimationEquipmentRequirement.Unarmed);
        RunAssigned(Creature);

        _service.OnEquipmentChanging();
        RunDeferred();

        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty();
        Token().Should().BeEmpty();
        Played().Should().Equal((Animation.PointForward, 1f, 2f));
    }

    [Test]
    public void PlayNativePreview_claims_the_token_and_plays_the_animation_when_the_closure_runs()
    {
        var token = _service.PlayNativePreview(Creature, Animation.SaberThrow, 1.5f);

        token.Should().NotBeNullOrEmpty();
        Token().Should().Be(token);
        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty("a native one-shot installs no authored mappings");
        Played().Should().BeEmpty();

        RunAssigned(Creature);

        Played().Should().Equal((Animation.SaberThrow, 1.5f, 0f));
        Token().Should().Be(token, "the preview keeps the ownership it claimed");
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void PlayNativePreview_rejects_a_speed_the_engine_cannot_play(float speed)
    {
        Action preview = () => _service.PlayNativePreview(Creature, Animation.SaberThrow, speed);

        preview.Should().Throw<ArgumentOutOfRangeException>();
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void PlayNativePreview_of_a_throw_suspends_the_pistol_remap_and_keeps_its_ownership()
    {
        SetLocalBool(Creature, "PISTOL_ANIM_REMAP_ACTIVE", true);
        var token = _service.PlayNativePreview(Creature, Animation.ThrowGrenade);

        RunAssigned(Creature);

        Token().Should().Be(token, "the throw must not release the token the preview just claimed");
        _engine.CallsTo("ReplaceObjectAnimation").Should().ContainSingle(call => (string)call.Arguments[1]! == "throwr");
        Played().Should().Equal((Animation.ThrowGrenade, 1f, 0f));
    }

    [Test]
    public void A_native_preview_a_newer_one_replaced_does_not_play()
    {
        _service.PlayNativePreview(Creature, Animation.SaberThrow);
        var second = _service.PlayNativePreview(Creature, Animation.LoopingPause);

        RunAssigned(Creature);

        Played().Should().Equal((Animation.LoopingPause, 1f, 0f));
        Token().Should().Be(second);
    }

    [Test]
    public void A_native_preview_expires_after_ten_seconds()
    {
        _service.PlayNativePreview(Creature, Animation.SaberThrow);

        RunTimers(10f);

        Token().Should().BeEmpty();
    }

    [Test]
    public void An_equipment_change_does_nothing_when_the_creature_owns_no_pose()
    {
        _service.OnEquipmentChanging();

        _engine.CallsTo("AssignCommand").Should().BeEmpty();
        _engine.CallsTo("ReplaceObjectAnimation").Should().BeEmpty();
    }

    [Test]
    public void An_equipment_change_releases_a_weapon_held_pose_and_previews_the_idle_pause()
    {
        _rifleInHand = true;
        _service.Play(Creature, Clip());
        RunAssigned(Creature);
        _locals.ClearCalls();

        _service.OnEquipmentChanging();

        Mappings().Values.Should().OnlyContain(value => value == string.Empty, "the hold mappings match the old weapon");
        var token = Token();
        token.Should().NotBeEmpty("the idle pause preview claims a new native token");
        RunAssigned(Creature);
        Played().Should().Equal((Animation.LoopingPause, 1f, 0f));
    }

    [Test]
    public void An_equipment_change_leaves_an_unrestricted_unheld_pose_alone()
    {
        var token = _service.Play(Creature, Clip());
        _locals.ClearCalls();

        _service.OnEquipmentChanging();

        Token().Should().Be(token);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }
}
