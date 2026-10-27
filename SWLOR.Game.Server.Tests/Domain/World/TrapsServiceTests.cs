using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the trap instance: the engine (NWScript, the player and visibility plugins) is replaced by recording
/// fakes with real local variables and a map of locations, and the database by an in-memory one. The traps' owners
/// and the creatures around them are not reported as players, which keeps the lookups that read the perks and the
/// stored player of a real player off the database; those lookups are exercised in the engine tests.
/// </summary>
[TestFixture]
public class TrapsServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Owner = 7001;
    private const uint OtherOwner = 7002;
    private const uint Intruder = 7003;
    private const uint Observer = 7004;
    private const uint DmObserver = 7005;
    private const uint PlayerOwner = 7006;
    private const uint FirstArea = 800;
    private const uint SecondArea = 801;
    private const uint FirstMarker = 701;
    private const uint SecondMarker = 702;
    private const float Lifetime = 300f;

    private const VisualEffect MarkerEffect = VisualEffect.Vfx_Fnf_Smoke_Puff;
    private const VisualEffect TriggerEffect = VisualEffect.Vfx_Com_Blood_Spark_Medium;

    // A server's facades resolve from a container; the services this one reaches through their static facades
    // (the DM presence check, say) may be converted, so the fixture installs the server's registrations.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private FakeVisibilityPlugin _visibility = null!;
    private InMemoryDBService _db = null!;
    private TrapsWithTraining _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _playerScope = null!;
    private IDisposable _visibilityScope = null!;

    private readonly Dictionary<nint, (uint Area, float Position)> _locations = new();
    private readonly HashSet<uint> _invalidObjects = new();
    private nint _nextLocation;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _locations.Clear();
        _invalidObjects.Clear();
        _nextLocation = 100;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && !_invalidObjects.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetAreaFromLocation", arguments => LocationOf((Location)arguments[0]!).Area);
        _engine.SetReturnHandler("GetDistanceBetweenLocations", arguments =>
            Math.Abs(LocationOf((Location)arguments[0]!).Position - LocationOf((Location)arguments[1]!).Position));
        _engine.SetReturn("GetCurrentHitPoints", 100);
        _engine.SetReturnSequence("CreateObject", FirstMarker, SecondMarker, 703u, 704u);
        _engine.SetReturnHandler("GetObjectUUID", arguments => "uuid-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is PlayerOwner);
        _playerPlugin = new FakePlayerPlugin();
        _visibility = new FakeVisibilityPlugin();
        _db = new InMemoryDBService();
        _service = new TrapsWithTraining(_db);

        _engineScope = NWScriptRuntime.Override(_engine);
        _playerScope = PlayerPluginRuntime.Override(_playerPlugin);
        _visibilityScope = VisibilityPluginRuntime.Override(_visibility);
    }

    [TearDown]
    public void TearDown()
    {
        _visibilityScope.Dispose();
        _playerScope.Dispose();
        _engineScope.Dispose();
        _facadeScope.Dispose();
    }

    /// <summary>
    /// The trap service with the Trapcraft training of each player answered from a list (the real lookup reads the perks
    /// of the stored player, which needs the database) and the bleed of a failed disarm recorded (the real one reads the
    /// native effect list).
    /// </summary>
    private sealed class TrapsWithTraining : TrapsService
    {
        public TrapsWithTraining(IDBService db)
            : base(db)
        {
        }

        public HashSet<uint> TrainedPlayers { get; } = new();

        /// <summary>The bleeds a failed disarm applied: the status effect system reads the native effect list.</summary>
        public List<(uint Owner, uint Victim)> Bleeds { get; } = new();

        protected override bool CanUseTrapTier(uint player, int tier) => TrainedPlayers.Contains(player);

        protected override void ApplyBleed(uint owner, uint victim) => Bleeds.Add((owner, victim));
    }

    /// <summary>A location the fake engine knows: in an area, at a position along a line.</summary>
    private Location At(float position, uint area = FirstArea)
    {
        var location = EngineStructures.Create<Location>(_nextLocation++);
        _locations[location.Handle] = (area, position);
        return location;
    }

    private (uint Area, float Position) LocationOf(Location location) =>
        _locations.TryGetValue(location.Handle, out var known) ? known : (ObjectInvalid, 0f);

    private List<string> MessagesTo(uint player) =>
        _engine.CallsTo("SendMessageToPC")
            .Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => (string)call.Arguments[1]!)
            .ToList();

    private bool PlacePerkTrap(uint owner, Location location) =>
        _service.TryPlaceTrap(owner, location, 14, CombatDamageType.Physical, typeof(BleedStatusEffect), 30, TriggerEffect, MarkerEffect);

    private Action LastDelayed() => (Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!;

    private bool IsMarkerDestroyed(uint marker) =>
        _engine.CallsTo("DestroyObject").Any(call => (uint)call.Arguments[0]! == marker && call.Arguments.Count > 1 && (float)call.Arguments[1]! == 0f);

    // ---- capacity

    [Test]
    public void GetTrapCapacity_is_one_trap_without_any_bonus()
    {
        _service.GetTrapCapacity(Owner).Should().Be(1);
    }

    // ---- placing a perk trap

    [Test]
    public void TryPlaceTrap_puts_a_marker_on_the_ground_for_five_minutes_and_reports_success()
    {
        var location = At(0f);

        PlacePerkTrap(Owner, location).Should().BeTrue();

        var created = _engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        created.Arguments[0].Should().Be(ObjectType.Placeable);
        created.Arguments[1].Should().Be("trap_marker");
        created.Arguments[2].Should().BeSameAs(location);
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments.Should().Equal(FirstMarker, Lifetime);
        _engine.CallsTo("SetPlotFlag").Should().ContainSingle().Which.Arguments.Should().Equal(FirstMarker, true);
    }

    [Test]
    public void TryPlaceTrap_arms_the_trap_after_three_seconds()
    {
        PlacePerkTrap(Owner, At(0f));

        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(3.0f);
    }

    [Test]
    public void TryPlaceTrap_fails_and_tells_the_owner_when_no_marker_can_be_created()
    {
        var offTheMap = EngineStructures.Create<Location>(9999);

        PlacePerkTrap(Owner, offTheMap).Should().BeFalse();

        MessagesTo(Owner).Should().Equal("The trap could not be placed.");
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _service.GetTrapCapacity(Owner).Should().Be(1);
    }

    [Test]
    public void TryPlaceTrap_fails_without_a_marker_effect()
    {
        var placed = _service.TryPlaceTrap(Owner, At(0f), 14, CombatDamageType.Physical, typeof(BleedStatusEffect), 30,
            TriggerEffect, VisualEffect.None);

        placed.Should().BeFalse();
        MessagesTo(Owner).Should().Equal("The trap could not be placed.");
    }

    [Test]
    public void TryPlaceTrap_refuses_a_spot_within_three_meters_of_anyones_live_trap()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        PlacePerkTrap(OtherOwner, At(3f)).Should().BeFalse();

        MessagesTo(OtherOwner).Should().ContainSingle().Which.Should().Contain("Another trap is placed too close to that spot.");
        _engine.CallsTo("CreateObject").Should().ContainSingle("the second trap never got a marker");
    }

    [Test]
    public void TryPlaceTrap_allows_a_spot_just_past_three_meters()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        PlacePerkTrap(OtherOwner, At(3.5f)).Should().BeTrue();
    }

    [Test]
    public void TryPlaceTrap_allows_the_same_spot_in_another_area()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        PlacePerkTrap(OtherOwner, At(0f, SecondArea)).Should().BeTrue();
    }

    [Test]
    public void TryPlaceTrap_beyond_the_capacity_deactivates_the_oldest_trap_of_the_owner()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        PlacePerkTrap(Owner, At(10f)).Should().BeTrue();

        IsMarkerDestroyed(FirstMarker).Should().BeTrue("the oldest trap's marker is removed");
        IsMarkerDestroyed(SecondMarker).Should().BeFalse();
        MessagesTo(Owner).Should().Equal("Your oldest trap deactivates as you place a new one.");
    }

    [Test]
    public void A_deactivated_trap_no_longer_blocks_its_spot()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();
        PlacePerkTrap(Owner, At(10f)).Should().BeTrue();

        PlacePerkTrap(OtherOwner, At(0f)).Should().BeTrue();
    }

    [Test]
    public void Each_instance_keeps_its_own_traps()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();
        var other = new TrapsService(_db);

        other.TryPlaceTrap(OtherOwner, At(0f), 14, CombatDamageType.Physical, typeof(BleedStatusEffect), 30, TriggerEffect, MarkerEffect)
            .Should().BeTrue("state lives on the instance, not in static fields");
    }

    // ---- placing a kit trap

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public void TryPlaceKitTrap_refuses_a_tier_the_kits_do_not_have(int tier)
    {
        _service.TryPlaceKitTrap(Owner, At(0f), tier).Should().BeFalse();

        _engine.CallsTo("CreateObject").Should().BeEmpty();
        MessagesTo(Owner).Should().BeEmpty();
    }

    [Test]
    public void TryPlaceKitTrap_hides_the_trap_from_everyone_but_its_owner()
    {
        var location = At(0f);

        _service.TryPlaceKitTrap(Owner, location, 2).Should().BeTrue();

        var created = _engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        created.Arguments[1].Should().Be("espn_trap");
        created.Arguments[2].Should().BeSameAs(location);
        created.Arguments[4].Should().Be("espn_trap");
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments.Should().Equal(FirstMarker, Lifetime);
        _visibility.CallsTo("SetVisibilityOverride").Select(call => (call.Arguments[0], call.Arguments[1], call.Arguments[2])).Should().Equal(
            ((object?)ObjectInvalid, (object?)FirstMarker, (object?)VisibilityType.Hidden),
            ((object?)Owner, (object?)FirstMarker, (object?)VisibilityType.Visible));
        MessagesTo(Owner).Should().BeEmpty("revealing the trap to its owner is silent");
    }

    [Test]
    public void TryPlaceKitTrap_tags_the_marker_with_the_id_the_disarm_looks_up()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();

        _locals.String(FirstMarker, "ESPIONAGE_TRAP_ID").Should().NotBeEmpty();
        Guid.TryParse(_locals.String(FirstMarker, "ESPIONAGE_TRAP_ID"), out _).Should().BeTrue();
    }

    [Test]
    public void TryPlaceKitTrap_arms_the_trap_after_three_seconds()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 3);

        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(3.0f);
    }

    [Test]
    public void TryPlaceKitTrap_keeps_the_kit_when_the_marker_cannot_be_created()
    {
        _engine.SetReturnSequence("CreateObject", ObjectInvalid);

        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeFalse();

        MessagesTo(Owner).Should().Equal("The trap could not be placed. Your kit has not been consumed.");
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void TryPlaceKitTrap_obeys_the_same_spacing_as_the_perk_traps()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        _service.TryPlaceKitTrap(OtherOwner, At(2f), 1).Should().BeFalse();

        MessagesTo(OtherOwner).Should().ContainSingle().Which.Should().Contain("too close");
        _engine.CallsTo("CreateObject").Should().ContainSingle();
    }

    [Test]
    public void TryPlaceKitTrap_accepts_every_tier_from_one_to_five()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var service = new TrapsService(_db);
            _engine.SetReturnSequence("CreateObject", (uint)(900 + tier));

            service.TryPlaceKitTrap(Owner, At(tier * 100f), tier).Should().BeTrue($"tier {tier}");
        }
    }

    // ---- clearing

    [Test]
    public void ClearTraps_deactivates_every_trap_of_the_owner_and_frees_their_spots()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        _service.ClearTraps(Owner);

        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        PlacePerkTrap(OtherOwner, At(0f)).Should().BeTrue();
    }

    [Test]
    public void ClearTraps_leaves_the_traps_of_other_owners_alone()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();
        PlacePerkTrap(OtherOwner, At(10f)).Should().BeTrue();

        _service.ClearTraps(Owner);

        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        IsMarkerDestroyed(SecondMarker).Should().BeFalse();
    }

    [Test]
    public void ClearTraps_does_nothing_for_an_owner_without_traps()
    {
        var clear = () => _service.ClearTraps(Owner);

        clear.Should().NotThrow();
        _engine.CallsTo("DestroyObject").Should().BeEmpty();
    }

    [Test]
    public void ClearTraps_skips_the_markers_the_engine_already_removed()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();
        _invalidObjects.Add(FirstMarker);

        _service.ClearTraps(Owner);

        IsMarkerDestroyed(FirstMarker).Should().BeFalse("a marker that is already gone is not destroyed again");
    }

    // ---- the proximity checks

    [Test]
    public void An_armed_trap_with_nobody_near_checks_again_every_second()
    {
        PlacePerkTrap(Owner, At(0f));

        LastDelayed()();

        var next = _engine.CallsTo("DelayCommand").Last();
        next.Arguments[0].Should().Be(1.0f);
        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    [Test]
    public void A_trap_expires_when_its_lifetime_runs_out_and_removes_its_marker()
    {
        PlacePerkTrap(Owner, At(0f));

        for (var tick = 0; tick < 400 && !IsMarkerDestroyed(FirstMarker); tick++)
            LastDelayed()();

        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        _engine.CallsTo("DelayCommand").Count.Should().BeInRange(298, 300, "one check a second for the rest of the five minutes");
        PlacePerkTrap(OtherOwner, At(0f)).Should().BeTrue("an expired trap frees its spot");
    }

    [Test]
    public void A_trap_expires_when_its_owner_leaves()
    {
        PlacePerkTrap(Owner, At(0f));
        _invalidObjects.Add(Owner);

        LastDelayed()();

        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        _engine.CallsTo("DelayCommand").Should().ContainSingle("no further check is scheduled");
    }

    [Test]
    public void A_deactivated_trap_stops_checking()
    {
        PlacePerkTrap(Owner, At(0f));
        var firstCheck = LastDelayed();
        _service.ClearTraps(Owner);
        _engine.ClearCalls();

        firstCheck();

        _engine.Calls.Should().BeEmpty();
    }

    // ---- going off

    private void HostileCreatureAppears(uint creature = Intruder)
    {
        _engine.SetReturnSequence("GetFirstObjectInShape", creature);
        _engine.SetReturn("GetIsReactionTypeHostile", true);
    }

    [Test]
    public void A_trap_goes_off_when_a_hostile_creature_comes_within_two_meters()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();
        var checks = _engine.CallsTo("DelayCommand").Count;

        LastDelayed()();

        _engine.CallsTo("GetFirstObjectInShape").First().Arguments.Take(3).Should().Equal(Shape.Sphere, 2.0f, _engine.CallsTo("GetFirstObjectInShape").First().Arguments[2]);
        _engine.CallsTo("GetFirstObjectInShape").First().Arguments[1].Should().Be(2.0f);
        IsMarkerDestroyed(FirstMarker).Should().BeTrue("a trap that goes off is spent");
        _engine.CallsTo("DelayCommand").Should().HaveCount(checks, "a spent trap schedules no further check");
        _engine.CallsTo("ApplyEffectAtLocation").Should().NotBeEmpty("the blast shows its area effect");
    }

    [Test]
    public void The_blast_shows_the_area_effect_the_perk_trap_was_built_for()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();

        LastDelayed()();

        _engine.CallsTo("EffectVisualEffect").Select(call => call.Arguments[0]).Should().Contain(VisualEffect.Vfx_Fnf_Smoke_Puff);
    }

    [Test]
    public void A_spent_trap_frees_its_spot_and_its_owners_capacity()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();
        LastDelayed()();
        _engine.ClearCalls();

        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();

        MessagesTo(Owner).Should().BeEmpty("nothing had to be deactivated to make room");
    }

    [Test]
    public void A_creature_that_is_dead_or_not_hostile_does_not_set_a_trap_off()
    {
        PlacePerkTrap(Owner, At(0f));
        _engine.SetReturnSequence("GetFirstObjectInShape", Intruder);
        _engine.SetReturn("GetIsReactionTypeHostile", false);

        LastDelayed()();

        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
        _engine.CallsTo("DelayCommand").Last().Arguments[0].Should().Be(1.0f);
    }

    [Test]
    public void A_dead_creature_does_not_set_a_trap_off()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == Intruder);

        LastDelayed()();

        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    [Test]
    public void A_creature_with_no_hit_points_left_does_not_set_a_trap_off()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();
        _engine.SetReturnHandler("GetCurrentHitPoints", arguments => (uint)arguments[0]! == Intruder ? 0 : 100);

        LastDelayed()();

        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    private static Player StoredPlayer(uint creature)
    {
        var player = new Player("uuid-" + creature);
        player.Skills[SkillType.Espionage] = new PlayerSkill { Rank = 40 };
        return player;
    }

    [Test]
    public void A_player_owner_earns_espionage_xp_when_a_creature_that_is_not_a_player_triggers_the_trap()
    {
        _db.With(StoredPlayer(PlayerOwner));
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == PlayerOwner);
        PlacePerkTrap(PlayerOwner, At(0f));
        HostileCreatureAppears();

        LastDelayed()();

        _db.Reads.Should().Equal("uuid-" + PlayerOwner);
        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
    }

    [Test]
    public void A_trap_set_off_by_a_player_earns_no_xp()
    {
        _db.With(StoredPlayer(PlayerOwner));
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == PlayerOwner);
        PlacePerkTrap(PlayerOwner, At(0f));
        HostileCreatureAppears(Observer);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is PlayerOwner or Observer);

        LastDelayed()();

        _db.Reads.Should().BeEmpty();
        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
    }

    [Test]
    public void A_trap_owned_by_a_creature_that_is_not_a_player_earns_no_xp()
    {
        PlacePerkTrap(Owner, At(0f));
        HostileCreatureAppears();

        LastDelayed()();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void A_trap_owned_by_a_DM_earns_no_xp()
    {
        PlacePerkTrap(PlayerOwner, At(0f));
        HostileCreatureAppears();
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == PlayerOwner);

        LastDelayed()();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void A_kit_trap_goes_off_with_its_blood_spark_and_earns_the_practice_xp_of_its_tier()
    {
        _db.With(StoredPlayer(PlayerOwner));
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == PlayerOwner);
        _service.TryPlaceKitTrap(PlayerOwner, At(0f), 3).Should().BeTrue();
        HostileCreatureAppears();

        LastDelayed()();

        _db.Reads.Should().Equal("uuid-" + PlayerOwner);
        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        _engine.CallsTo("ApplyEffectAtLocation").Should().NotBeEmpty();
    }

    [Test]
    public void A_kit_trap_keeps_checking_while_nobody_is_near()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        PlayersOnline();

        LastDelayed()();

        _engine.CallsTo("DelayCommand").Last().Arguments[0].Should().Be(1.0f);
        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    // ---- who can use a tier

    [TestCase(0, false, 1, false)]
    [TestCase(1, false, 1, true)]
    [TestCase(1, false, 2, false)]
    [TestCase(3, false, 3, true)]
    [TestCase(4, false, 4, true)]
    [TestCase(4, false, 3, true)]
    [TestCase(5, false, 5, false)]
    [TestCase(5, true, 5, true)]
    [TestCase(0, true, 5, true)]
    [TestCase(5, true, 6, false)]
    [TestCase(5, true, 0, false)]
    [TestCase(5, true, -1, false)]
    public void CanUseTrapTier_needs_the_trapcraft_rank_of_the_tier_and_master_saboteur_for_tier_five(
        int rank, bool masterSaboteur, int tier, bool expected)
    {
        _service.CanUseTrapTier(rank, masterSaboteur, tier).Should().Be(expected);
    }

    // ---- the concealed trap's detection sweep

    private void PlayersOnline(params uint[] players)
    {
        _engine.SetReturnSequence("GetFirstPC", players.Take(1).Select(player => (object)player).ToArray());
        _engine.SetReturnSequence("GetNextPC", players.Skip(1).Select(player => (object)player).ToArray());
    }

    private void EveryoneIsIn(uint area) => _engine.SetReturnHandler("GetArea", _ => area);

    [Test]
    public void The_sweep_skips_players_in_another_area_and_players_controlled_by_a_DM()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        PlayersOnline(Observer, DmObserver);
        _engine.SetReturnHandler("GetArea", arguments => (uint)arguments[0]! == Observer ? SecondArea : FirstArea);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == DmObserver);

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty("nobody was able to notice the trap");
        MessagesTo(Observer).Should().BeEmpty();
        MessagesTo(DmObserver).Should().BeEmpty();
    }

    [Test]
    public void The_sweep_does_nothing_once_the_marker_is_gone()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        PlayersOnline(Observer);
        EveryoneIsIn(FirstArea);
        _invalidObjects.Add(FirstMarker);

        LastDelayed()();

        _engine.CallsTo("GetFirstPC").Should().BeEmpty();
    }

    [Test]
    public void The_sweep_does_not_run_for_the_perk_traps()
    {
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue();
        PlayersOnline(Observer);

        LastDelayed()();

        _engine.CallsTo("GetFirstPC").Should().BeEmpty();
    }

    // ---- disarming

    private void Uses(uint marker, uint user)
    {
        _engine.SetReturn("OBJECT_SELF", marker);
        _engine.SetReturn("GetLastUsedBy", user);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is PlayerOwner or Observer or OtherOwner);
        _service.DisarmTrap();
    }

    [Test]
    public void DisarmTrap_ignores_a_user_who_is_not_a_player()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _engine.ClearCalls();

        Uses(FirstMarker, Intruder);

        MessagesTo(Intruder).Should().BeEmpty();
        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    [Test]
    public void DisarmTrap_ignores_a_DM()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Observer);

        Uses(FirstMarker, Observer);

        MessagesTo(Observer).Should().BeEmpty();
        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
    }

    [Test]
    public void DisarmTrap_ignores_a_marker_that_is_not_a_known_live_trap()
    {
        Uses(FirstMarker, Observer);

        MessagesTo(Observer).Should().BeEmpty();
    }

    [Test]
    public void DisarmTrap_lets_the_owner_dismantle_their_own_trap_without_recovering_the_kit()
    {
        _service.TryPlaceKitTrap(OtherOwner, At(0f), 1).Should().BeTrue();

        Uses(FirstMarker, OtherOwner);

        MessagesTo(OtherOwner).Should().Equal("You dismantle your own trap. The kit is not recovered.");
        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        PlacePerkTrap(Owner, At(0f)).Should().BeTrue("the dismantled trap no longer blocks its spot");
    }

    [Test]
    public void DisarmTrap_refuses_a_user_without_the_trapcraft_for_the_tier()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 2).Should().BeTrue();

        Uses(FirstMarker, Observer);

        MessagesTo(Observer).Should().ContainSingle().Which.Should().Contain("You lack the Trapcraft expertise to disarm this trap.");
        IsMarkerDestroyed(FirstMarker).Should().BeFalse();
        _engine.CallsTo("d100").Should().BeEmpty("no roll is made without the training");
    }

    /// <summary>
    /// A trained user tries to disarm a kit trap of a tier: the engine answers the user's Perception modifier and the roll
    /// of the d100. The user is a dead player so that the XP award, which reads the stored player, ends at its first check.
    /// </summary>
    private bool DisarmSucceeds(int tier, int perceptionModifier, int roll)
    {
        _engine.SetReturn("GetAbilityModifier", perceptionModifier);
        _engine.SetReturn("d100", roll);
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == Observer);
        _service.TrainedPlayers.Add(Observer);
        _db.With(StoredPlayer(Observer));
        _service.TryPlaceKitTrap(Owner, At(0f), tier).Should().BeTrue();

        Uses(FirstMarker, Observer);

        var messages = MessagesTo(Observer);
        messages.Should().ContainSingle();
        return messages.Single() == "You disarm the trap.";
    }

    [TestCase(1, 0, 40)]
    [TestCase(1, 5, 50)]
    [TestCase(3, 10, 40)]
    [TestCase(2, -3, 24)]
    [TestCase(5, 0, 5)]
    [TestCase(1, -100, 5)]
    [TestCase(1, 100, 95)]
    public void The_disarm_chance_is_fifty_plus_twice_perception_minus_ten_a_tier_within_five_and_ninety_five(
        int tier, int perceptionModifier, int expectedChance)
    {
        DisarmSucceeds(tier, perceptionModifier, expectedChance).Should().BeTrue("a roll equal to the chance disarms the trap");
    }

    [TestCase(1, 0, 40)]
    [TestCase(1, 5, 50)]
    [TestCase(3, 10, 40)]
    [TestCase(5, 0, 5)]
    [TestCase(1, -100, 5)]
    [TestCase(1, 100, 95)]
    public void A_roll_one_past_the_chance_sets_the_trap_off_instead(int tier, int perceptionModifier, int expectedChance)
    {
        DisarmSucceeds(tier, perceptionModifier, expectedChance + 1).Should().BeFalse();
    }

    [Test]
    public void A_successful_disarm_clears_the_trap()
    {
        DisarmSucceeds(1, 0, 1).Should().BeTrue();

        IsMarkerDestroyed(FirstMarker).Should().BeTrue();
        PlacePerkTrap(OtherOwner, At(0f)).Should().BeTrue();
    }

    [Test]
    public void Disarming_a_trap_of_a_creature_hostile_to_the_user_earns_espionage_xp()
    {
        _engine.SetReturn("GetIsEnemy", true);

        DisarmSucceeds(1, 0, 1);

        _db.Reads.Should().Equal("uuid-" + Observer);
    }

    [Test]
    public void Disarming_a_trap_whose_creature_owner_is_not_hostile_earns_no_xp()
    {
        _engine.SetReturn("GetIsEnemy", false);

        DisarmSucceeds(1, 0, 1);

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Disarming_another_players_trap_earns_no_xp()
    {
        _engine.SetReturn("GetIsEnemy", true);
        _engine.SetReturn("GetAbilityModifier", 0);
        _engine.SetReturn("d100", 1);
        _service.TrainedPlayers.Add(Observer);
        _service.TryPlaceKitTrap(OtherOwner, At(0f), 1).Should().BeTrue();

        Uses(FirstMarker, Observer);

        _db.Reads.Should().BeEmpty();
        MessagesTo(Observer).Should().Equal("You disarm the trap.");
    }

    [Test]
    public void A_failed_disarm_sets_the_trap_off_on_the_user_with_the_damage_of_its_tier()
    {
        DisarmSucceeds(2, 0, 100).Should().BeFalse();

        IsMarkerDestroyed(FirstMarker).Should().BeTrue("the trap is spent");
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Owner, "the damage comes from the owner of the trap");
        ((Action)assigned.Arguments[1]!)();
        _engine.CallsTo("EffectDamage").Should().ContainSingle().Which.Arguments.Take(2).Should().Equal(30, DamageType.Piercing);
        _engine.CallsTo("ApplyEffectToObject").Select(call => (call.Arguments[0], call.Arguments[2])).Should().Contain(
            ((object?)DurationType.Instant, (object?)Observer));
    }

    [Test]
    public void A_failed_disarm_also_shows_a_blood_spark_on_the_user_and_says_so()
    {
        DisarmSucceeds(1, 0, 100).Should().BeFalse();

        _engine.CallsTo("EffectVisualEffect").Select(call => call.Arguments[0]).Should().Contain(VisualEffect.Vfx_Com_Blood_Spark_Medium);
        MessagesTo(Observer).Single().Should().Contain("You set the trap off while trying to disarm it.");
        _service.Bleeds.Should().Equal((Owner, Observer));
    }

    [TestCase(1, 20)]
    [TestCase(2, 30)]
    [TestCase(3, 40)]
    [TestCase(4, 52)]
    [TestCase(5, 66)]
    public void The_damage_of_a_failed_disarm_is_the_damage_of_the_kit_tier(int tier, int expectedDamage)
    {
        DisarmSucceeds(tier, 0, 100).Should().BeFalse();

        ((Action)_engine.CallsTo("AssignCommand").Single().Arguments[1]!)();
        _engine.CallsTo("EffectDamage").Single().Arguments[0].Should().Be(expectedDamage);
    }

    // ---- the concealed trap's reveal

    private void PlayerStandsAt(uint player, Location location)
    {
        _engine.SetReturnHandler("GetLocation", arguments => (uint)arguments[0]! == player ? location : At(0f));
    }

    [Test]
    public void A_trained_player_close_enough_spots_the_trap_and_is_told_so()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(Observer);
        PlayersOnline(Observer);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(Observer, At(5.9f));

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Select(call => (call.Arguments[0], call.Arguments[1], call.Arguments[2])).Should().Equal(
            ((object?)Observer, (object?)FirstMarker, (object?)VisibilityType.Visible));
        MessagesTo(Observer).Should().ContainSingle().Which.Should().Contain("You spot a concealed trap nearby.");
    }

    [Test]
    public void A_trained_player_past_six_meters_does_not_spot_the_trap()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(Observer);
        PlayersOnline(Observer);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(Observer, At(6.5f));

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
        MessagesTo(Observer).Should().BeEmpty();
    }

    [Test]
    public void A_player_without_the_training_does_not_spot_the_trap_however_close()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        PlayersOnline(Observer);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(Observer, At(0f));

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
    }

    [Test]
    public void A_player_spots_the_same_trap_only_once()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(Observer);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(Observer, At(1f));

        PlayersOnline(Observer);
        LastDelayed()();
        PlayersOnline(Observer);
        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().ContainSingle();
        MessagesTo(Observer).Should().ContainSingle();
    }

    [Test]
    public void The_sweep_skips_a_DM_who_is_possessing_a_creature_although_their_avatar_is_not_reported_as_a_DM()
    {
        const uint avatar = 7007;
        const uint possessed = 7008;
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(avatar);
        PlayersOnline(avatar);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(avatar, At(1f));
        // GetFirstPC keeps returning the avatar; only the creature the player controls is possessed.
        _playerPlugin.SetReturnHandler("GetGameObject", arguments => (uint)arguments[0]! == avatar ? possessed : arguments[0]);
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => (uint)arguments[0]! == possessed);

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
        MessagesTo(avatar).Should().BeEmpty();
    }

    [Test]
    public void The_sweep_skips_a_DM_whose_controlled_creature_is_reported_as_a_DM()
    {
        const uint avatar = 7007;
        const uint controlled = 7008;
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(avatar);
        PlayersOnline(avatar);
        EveryoneIsIn(FirstArea);
        PlayerStandsAt(avatar, At(1f));
        _playerPlugin.SetReturnHandler("GetGameObject", arguments => (uint)arguments[0]! == avatar ? controlled : arguments[0]);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == controlled);

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Should().BeEmpty();
    }

    [Test]
    public void The_sweep_reveals_the_trap_to_every_trained_player_in_range_and_never_to_a_DM()
    {
        _service.TryPlaceKitTrap(Owner, At(0f), 1).Should().BeTrue();
        _visibility.ClearCalls();
        _service.TrainedPlayers.Add(Observer);
        _service.TrainedPlayers.Add(OtherOwner);
        _service.TrainedPlayers.Add(DmObserver);
        PlayersOnline(Observer, OtherOwner, DmObserver);
        EveryoneIsIn(FirstArea);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == DmObserver);
        var nearby = At(2f);
        _engine.SetReturnHandler("GetLocation", _ => nearby);

        LastDelayed()();

        _visibility.CallsTo("SetVisibilityOverride").Select(call => call.Arguments[0]).Should().Equal(new object?[] { Observer, OtherOwner });
    }
}
