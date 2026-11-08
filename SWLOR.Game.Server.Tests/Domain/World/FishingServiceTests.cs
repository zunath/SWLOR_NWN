using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the fishing instance. The engine is a recording fake with real local variables, the stored player is
/// in memory, and the random rolls are seeded: each test that depends on a roll finds a seed whose rolls (replayed
/// with <see cref="System.Random"/> in the order the service draws them) give the outcome it wants, so the order and
/// the number of draws are part of what the tests pin.
/// </summary>
[TestFixture]
[NonParallelizable]
public class FishingServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 5001;
    private const uint Point = 5002;
    private const uint Rod = 5003;
    private const uint Module = 5004;
    private const uint Bystander = 5005;
    private const string PlayerId = "fisher-1";
    private const string RodTag = "FISHING_ROD";
    private const int CavernSkillRank = 35;

    private static readonly string YewRod = FishingRodType.Yew.GetAttribute<FishingRodType, FishingRodAttribute>().Resref;
    private static readonly string TroutBall = FishingBaitType.TroutBall.GetAttribute<FishingBaitType, FishingBaitAttribute>().Resrefs.First();

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private InMemoryDBService _db = null!;
    private TestFishing _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _playerScope = null!;

    private readonly HashSet<uint> _invalid = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private bool _isPlayer = true;
    private bool _isDm;
    private bool _isPossessed;
    private bool _playerDead;
    private bool _pointDead;
    private bool _inCombat;
    private bool _rodEquipped = true;
    private string _rodTag = RodTag;
    private float _distance = 3f;
    private Vector3 _position = new(1f, 2f, 3f);
    private int _willpower = 20;

    /// <summary>The fishing service with the item name and the angler's perk effects answered by the test, and the fights it opens recorded.</summary>
    private sealed class TestFishing : FishingService
    {
        public TestFishing(IDBService db)
            : base(db, FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<ICacheService>(), FacadeForwarder.For<IFishingFightsService>(), FacadeForwarder.For<IMessagingService>(), FacadeForwarder.For<INamedAnimationService>(), FacadeForwarder.For<IPlayerNameService>(), FacadeForwarder.For<IStatService>())
        {
        }

        public List<FishingBite> Bites { get; } = new();

        public LineAnglerEffects Effects { get; } = new(TensionCapBonus: 3, ForecastTurns: 2);

        protected override string GetBaitName(string resref) => "Name of " + resref;

        protected override void StartFight(FishingBite bite) => Bites.Add(bite);

        protected override LineAnglerEffects GetAnglerEffects(uint player) => Effects;
    }

    // Fishing broadcasts through the Messaging facade, which resolves the service from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _invalid.Clear();
        _resrefs.Clear();
        _isPlayer = true;
        _isDm = false;
        _isPossessed = false;
        _playerDead = false;
        _pointDead = false;
        _inCombat = false;
        _rodEquipped = true;
        _rodTag = RodTag;
        _distance = 3f;
        _position = new Vector3(1f, 2f, 3f);
        _willpower = 20;
        _resrefs[Rod] = YewRod;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _playerPlugin = new FakePlayerPlugin();
        _db = new InMemoryDBService();
        _service = new TestFishing(_db);

        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && !_invalid.Contains((uint)arguments[0]!));
        _engine.SetReturn("OBJECT_SELF", Point);
        _engine.SetReturn("GetPlaceableLastClickedBy", Player);
        _engine.SetReturn("GetModule", Module);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == Player && _isPlayer);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Player && _isDm);
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => (uint)arguments[0]! == Player && _isPossessed);
        _engine.SetReturnHandler("GetIsDead", arguments => (uint)arguments[0]! == Player ? _playerDead : (uint)arguments[0]! == Point && _pointDead);
        _engine.SetReturnHandler("GetIsInCombat", arguments => (uint)arguments[0]! == Player && _inCombat);
        _engine.SetReturnHandler("GetItemInSlot", arguments => _rodEquipped && (uint)arguments[1]! == Player ? Rod : ObjectInvalid);
        _engine.SetReturnHandler("GetTag", arguments => (uint)arguments[0]! == Rod ? _rodTag : string.Empty);
        _engine.SetReturnHandler("GetResRef", arguments => _resrefs.TryGetValue((uint)arguments[0]!, out var resref) ? resref : string.Empty);
        _engine.SetReturnHandler("GetDistanceBetween", _ => _distance);
        _engine.SetReturnHandler("GetPosition", arguments => (uint)arguments[0]! == Player ? _position : new Vector3(9f, 9f, 9f));
        _engine.SetReturnHandler("GetObjectUUID", arguments => (uint)arguments[0]! == Player ? PlayerId : string.Empty);
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Creature);
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! == Player ? "Fisher" : string.Empty);
        _engine.SetReturnHandler("GetAbilityScore", _ => _willpower);
        _engine.SetReturnHandler("Vector3", arguments => new Vector3((float)arguments[0]!, (float)arguments[1]!, (float)arguments[2]!));

        _engineScope = NWScriptRuntime.Override(_engine);
        _playerScope = PlayerPluginRuntime.Override(_playerPlugin);
    }

    [TearDown]
    public void TearDown()
    {
        _facadeScope.Dispose();
        Random.ResetSeed();
        _playerScope.Dispose();
        _engineScope.Dispose();
    }

    // ---- helpers

    /// <summary>
    /// Caches the fishing data. The special fish are added to locations picked with the shared random generator, so
    /// the cache is built from a seed whose picks leave the locations these tests use alone.
    /// </summary>
    private void LoadFishing()
    {
        Random.SetSeed(CacheSeed);
        _service.CacheData();
    }

    private void LoadRod(FishingBaitType bait = FishingBaitType.TroutBall, int remaining = 40)
    {
        _engine.SetLocalInt(Rod, "LOADED_BAIT_TYPE", (int)bait);
        _engine.SetLocalInt(Rod, "REMAINING_BAIT", remaining);
    }

    private void PointAt(FishingLocationType location, int attemptsLeft = 4, bool initialized = true)
    {
        _engine.SetLocalInt(Point, "FISHING_LOCATION_ID", (int)location);
        if (initialized)
        {
            _engine.SetLocalBool(Point, "FISHING_POINT_INITIALIZED", true);
            _engine.SetLocalInt(Point, "FISHING_POINT_REMAINING_ATTEMPTS", attemptsLeft);
        }
    }

    private void StoredPlayer(int agricultureRank)
    {
        var player = new Player(PlayerId);
        player.Skills[SkillType.Agriculture] = new PlayerSkill { Rank = agricultureRank };
        _db.With(player);
    }

    private static readonly int CacheSeed = SeedWhere(random =>
    {
        var locations = Enum.GetValues<FishingLocationType>().Where(type => type != FishingLocationType.Invalid).ToList();
        var baitCount = Enum.GetValues<FishingBaitType>().Count(type => type != FishingBaitType.Invalid);
        var avoided = new[] { locations.IndexOf(FishingLocationType.ViscaraCavern), locations.IndexOf(FishingLocationType.ViscaraDeepwoods) };
        for (var special = 0; special < 6; special++)
        {
            var picked = random.Next(locations.Count);
            random.Next(baitCount);
            if (avoided.Contains(picked))
                return false;
        }

        return true;
    });

    private static int SeedWhere(Func<System.Random, bool> rolls)
    {
        for (var seed = 1; seed < 20000; seed++)
        {
            if (rolls(new System.Random(seed)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives the wanted rolls.");
    }

    private List<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Where(call => (uint)call.Arguments[0]! == Player).Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>Clicks the fishing point and returns the cast's wait time from the timing bar.</summary>
    private float Cast()
    {
        _service.ClickFishingPoint();
        return (float)_playerPlugin.CallsTo("StartGuiTimingBar").Single().Arguments[1]!;
    }

    private List<Action> ModuleActions() =>
        _engine.CallsTo("AssignCommand").Where(call => (uint)call.Arguments[0]! == Module).Select(call => (Action)call.Arguments[1]!).ToList();

    /// <summary>
    /// Lets the cast run to its end: the timer the cast scheduled fires and the service finishes fishing. Returns what
    /// the player was told while it finished.
    /// </summary>
    private List<string> WaitForTheCast()
    {
        var before = Messages().Count;
        ModuleActions().First()();
        var finish = (Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!;
        finish();
        return Messages().Skip(before).ToList();
    }

    // ---- data

    [Test]
    public void CacheData_makes_every_rod_resref_a_fishing_rod()
    {
        LoadFishing();
        var rod = 7001u;

        foreach (var type in Enum.GetValues<FishingRodType>().Where(type => type != FishingRodType.Invalid))
        {
            _resrefs[rod] = type.GetAttribute<FishingRodType, FishingRodAttribute>().Resref;
            _service.IsItemFishingRod(rod).Should().BeTrue(type.ToString());
        }

        _resrefs[rod] = "not_a_rod";
        _service.IsItemFishingRod(rod).Should().BeFalse();
    }

    [Test]
    public void CacheData_makes_every_bait_resref_a_bait()
    {
        LoadFishing();
        var bait = 7002u;

        foreach (var type in Enum.GetValues<FishingBaitType>().Where(type => type != FishingBaitType.Invalid))
        foreach (var resref in type.GetAttribute<FishingBaitType, FishingBaitAttribute>().Resrefs)
        {
            _resrefs[bait] = resref;
            _service.IsItemBait(bait).Should().BeTrue(resref);
            _service.GetBaitByResref(resref).Should().Be(type);
        }

        _resrefs[bait] = "not_a_bait";
        _service.IsItemBait(bait).Should().BeFalse();
        _service.GetBaitByResref("not_a_bait").Should().Be(FishingBaitType.Invalid);
    }

    [Test]
    public void Nothing_is_a_rod_or_a_bait_before_the_data_is_cached()
    {
        _service.IsItemFishingRod(Rod).Should().BeFalse();
        _service.IsItemBait(Rod).Should().BeFalse();
        _service.GetBaitByResref(TroutBall).Should().Be(FishingBaitType.Invalid);
    }

    [Test]
    public void GetFishAvailableAtLocation_lists_the_fish_a_location_shows_in_its_description()
    {
        LoadFishing();

        var fish = _service.GetFishAvailableAtLocation(FishingLocationType.ViscaraCavern);

        fish.Should().Contain("nebimonite");
        fish.Should().OnlyHaveUniqueItems();
        _service.GetFishAvailableAtLocation(FishingLocationType.ViscaraDeepwoods).Should().BeEmpty("the location has no fish of its own");
    }

    [Test]
    public void GetFishAvailableAtLocation_throws_for_a_location_without_a_definition()
    {
        LoadFishing();

        var lookup = () => _service.GetFishAvailableAtLocation(FishingLocationType.Invalid);

        lookup.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetLoadedBait_reads_the_bait_type_stored_on_the_rod()
    {
        _service.GetLoadedBait(Rod).Should().Be(FishingBaitType.Invalid);

        LoadRod(FishingBaitType.WormLure);

        _service.GetLoadedBait(Rod).Should().Be(FishingBaitType.WormLure);
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void GetFishingAnimation_picks_the_clip_with_the_wait_time(int seconds)
    {
        var clip = _service.GetFishingAnimation(seconds);

        clip.Duration.Should().Be(seconds);
        clip.Name.Should().Be("sw_fishing" + seconds);
    }

    [TestCase(5)]
    [TestCase(9)]
    public void GetFishingAnimation_refuses_a_wait_time_without_a_clip(int seconds)
    {
        var pick = () => _service.GetFishingAnimation(seconds);

        pick.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- clicking the point

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Clicking_the_point_is_for_players_only(bool isPlayer, bool isDm, bool isPossessed)
    {
        _isPlayer = isPlayer;
        _isDm = isDm;
        _isPossessed = isPossessed;

        _service.ClickFishingPoint();

        Messages().Should().Equal("Only players may fish.");
        _playerPlugin.CallsTo("StartGuiTimingBar").Should().BeEmpty();
    }

    [Test]
    public void Clicking_the_point_refuses_a_busy_player()
    {
        _engine.SetLocalBool(Player, "IS_BUSY", true);

        _service.ClickFishingPoint();

        Messages().Should().Equal("You are busy.");
    }

    [TestCase("dead")]
    [TestCase("combat")]
    public void Clicking_the_point_refuses_a_player_who_is_dead_or_fighting(string condition)
    {
        _playerDead = condition == "dead";
        _inCombat = condition == "combat";

        _service.ClickFishingPoint();

        Messages().Should().Equal("You cannot fish right now.");
    }

    [TestCase(false, RodTag)]
    [TestCase(true, "SOMETHING_ELSE")]
    public void Clicking_the_point_needs_a_fishing_rod_in_hand(bool equipped, string tag)
    {
        _rodEquipped = equipped;
        _rodTag = tag;

        _service.ClickFishingPoint();

        Messages().Should().Equal("A fishing rod must be equipped.");
    }

    [TestCase(FishingBaitType.Invalid, 10)]
    [TestCase(FishingBaitType.TroutBall, 0)]
    public void Clicking_the_point_needs_bait_on_the_rod(FishingBaitType bait, int remaining)
    {
        LoadRod(bait, remaining);

        _service.ClickFishingPoint();

        Messages().Should().Equal("Your fishing rod has no bait loaded. Use the fishing rod and target a bait to load some.");
    }

    [Test]
    public void Clicking_the_point_needs_the_player_to_stand_close()
    {
        LoadRod();
        _distance = 10.5f;

        _service.ClickFishingPoint();

        Messages().Should().Equal("You are too far away from the fishing point.");
    }

    [Test]
    public void Clicking_an_exhausted_point_frees_the_player()
    {
        LoadRod();
        _pointDead = true;

        _service.ClickFishingPoint();

        Messages().Should().Equal("This fishing point has been exhausted.");
        _locals.Int(Player, "IS_BUSY").Should().Be(0);
        _engine.CallsTo("DeleteLocalBool").Should().Contain(call => (uint)call.Arguments[0]! == Player && (string)call.Arguments[1]! == "IS_BUSY");
    }

    [Test]
    public void Clicking_a_new_point_rolls_its_attempts_before_the_wait_time()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern, initialized: false);
        var seed = SeedWhere(random => random.Next(10) == 3 && random.Next(3) == 2);
        Random.SetSeed(seed);

        var wait = Cast();

        _locals.Int(Point, "FISHING_POINT_REMAINING_ATTEMPTS").Should().Be(5 + 3);
        _locals.Int(Point, "FISHING_POINT_INITIALIZED").Should().Be(1);
        wait.Should().Be(6 + 2);
    }

    [Test]
    public void Clicking_a_point_already_rolled_keeps_its_attempts_and_draws_only_the_wait_time()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern, attemptsLeft: 2);
        var seed = SeedWhere(random => random.Next(3) == 1);
        Random.SetSeed(seed);

        var wait = Cast();

        _locals.Int(Point, "FISHING_POINT_REMAINING_ATTEMPTS").Should().Be(2);
        wait.Should().Be(6 + 1);
    }

    [Test]
    public void Clicking_the_point_starts_the_cast()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(SeedWhere(random => random.Next(3) == 0));

        var wait = Cast();

        wait.Should().Be(6);
        _locals.Float(Player, "FISHING_POSITION_X").Should().Be(1f);
        _locals.Float(Player, "FISHING_POSITION_Y").Should().Be(2f);
        _locals.Float(Player, "FISHING_POSITION_Z").Should().Be(3f);
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().NotBeEmpty();
        _locals.Object(Player, "FISHING_POINT").Should().Be(Point);
        _locals.Int(Player, "IS_BUSY").Should().Be(1);
        _locals.Int(Player, "BUSY_TYPE").Should().Be((int)ActivityStatusType.Fishing);
        _engine.CallsTo("ClearAllActions").Should().ContainSingle();
        Messages().Should().Equal("Fisher casts a line into the water.");
        _locals.String(Player, "FISHING_ANIMATION_TOKEN").Should().NotBeEmpty("the cast plays the fishing clip for its wait time");
    }

    [Test]
    public void Clicking_the_point_tells_the_players_nearby_about_the_cast_and_turns_to_the_water()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        _engine.SetReturnSequence("GetNearestCreature", Bystander, ObjectInvalid);
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! == Player ? "Fisher" : "Bystander");
        _engine.SetReturnHandler("GetDistanceBetween", arguments => (uint)arguments[1]! == Bystander ? 4f : _distance);

        _service.ClickFishingPoint();

        _engine.CallsTo("SendMessageToPC").Select(call => (uint)call.Arguments[0]!).Should().Contain(Bystander);
        _engine.CallsTo("AssignCommand").Should().Contain(call => (uint)call.Arguments[0]! == Player, "the player turns to face the point");
    }

    // ---- watching the cast

    [Test]
    public void The_cast_keeps_watching_while_the_player_stands_still()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var watch = ModuleActions().Last();
        _locals.ClearCalls();

        watch();

        var recheck = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        recheck.Arguments[0].Should().Be(0.1f);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void The_cast_ends_with_a_message_when_the_player_moves()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var watch = ModuleActions().Last();
        _locals.ClearCalls();
        _position = new Vector3(1f, 2.5f, 3f);

        watch();

        Messages().Should().Equal("You move and interrupt your cast.");
        _playerPlugin.CallsTo("StopGuiTimingBar").Should().ContainSingle();
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().BeEmpty();
        _locals.Int(Player, "IS_BUSY").Should().Be(0);
    }

    [TestCase("dead")]
    [TestCase("combat")]
    [TestCase("rod")]
    [TestCase("point")]
    public void The_cast_ends_quietly_when_the_player_can_no_longer_fish(string reason)
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var watch = ModuleActions().Last();
        _locals.ClearCalls();
        _playerDead = reason == "dead";
        _inCombat = reason == "combat";
        _rodEquipped = reason != "rod";
        _pointDead = reason == "point";

        watch();

        Messages().Should().BeEmpty();
        _playerPlugin.CallsTo("StopGuiTimingBar").Should().ContainSingle();
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().BeEmpty();
        _locals.Int(Player, "IS_BUSY").Should().Be(0);
    }

    [Test]
    public void The_cast_leaves_a_newer_activity_alone_when_it_ends()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var watch = ModuleActions().Last();
        _engine.SetLocalInt(Player, "BUSY_TYPE", (int)ActivityStatusType.UseItem);
        _playerDead = true;
        _locals.ClearCalls();

        watch();

        _playerPlugin.CallsTo("StopGuiTimingBar").Should().BeEmpty("another activity owns the progress bar");
        _locals.Int(Player, "IS_BUSY").Should().Be(1, "the newer activity stays busy");
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().BeEmpty();
    }

    [Test]
    public void The_watch_of_an_old_cast_does_nothing_after_a_new_cast_started()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var watch = ModuleActions().Last();
        _engine.SetLocalString(Player, "FISHING_ATTEMPT_ID", "a-newer-attempt");
        _position = new Vector3(5f, 5f, 5f);
        _locals.ClearCalls();

        watch();

        Messages().Should().BeEmpty();
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().Be("a-newer-attempt");
    }

    [Test]
    public void The_finish_timer_of_an_old_cast_does_nothing_after_a_new_cast_started()
    {
        LoadRod();
        PointAt(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        var timer = ModuleActions().First();
        timer();
        var finish = (Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!;
        _engine.SetLocalString(Player, "FISHING_ATTEMPT_ID", "a-newer-attempt");
        _locals.ClearCalls();

        finish();

        Messages().Should().BeEmpty();
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().Be("a-newer-attempt");
    }

    // ---- finishing

    private void ReadyToFish(FishingLocationType location, int attemptsLeft = 4, int rank = CavernSkillRank, int bait = 40)
    {
        LoadFishing();
        LoadRod(remaining: bait);
        PointAt(location, attemptsLeft);
        StoredPlayer(rank);
    }

    [Test]
    public void Finishing_after_the_player_moved_interrupts_the_cast()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _position = new Vector3(0f, 0f, 0f);

        var messages = WaitForTheCast();

        messages.Should().Equal("You move and interrupt your cast.");
        _locals.Int(Rod, "REMAINING_BAIT").Should().Be(40, "the bait is spent only once fishing begins");
    }

    [TestCase("dead")]
    [TestCase("combat")]
    public void Finishing_does_nothing_for_a_player_who_died_or_started_fighting(string reason)
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _playerDead = reason == "dead";
        _inCombat = reason == "combat";

        var messages = WaitForTheCast();

        messages.Should().BeEmpty();
        _locals.String(Player, "FISHING_ATTEMPT_ID").Should().BeEmpty();
        _locals.Int(Rod, "REMAINING_BAIT").Should().Be(40);
    }

    [Test]
    public void Finishing_does_nothing_for_a_player_whose_activity_was_replaced()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _engine.SetLocalInt(Player, "BUSY_TYPE", (int)ActivityStatusType.UseItem);

        var messages = WaitForTheCast();

        messages.Should().BeEmpty();
        _locals.Int(Player, "IS_BUSY").Should().Be(1, "the other activity keeps its busy state");
    }

    [Test]
    public void Finishing_at_an_exhausted_point_frees_the_player()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _pointDead = true;

        var messages = WaitForTheCast();

        messages.Should().Equal("This fishing point has been exhausted.");
        _locals.Int(Player, "IS_BUSY").Should().Be(0);
    }

    [Test]
    public void Finishing_without_the_rod_in_hand_reports_it()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _rodEquipped = false;

        var messages = WaitForTheCast();

        messages.Should().Equal("A fishing rod must be equipped.");
    }

    [Test]
    public void Finishing_with_the_bait_gone_reports_it()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _engine.SetLocalInt(Rod, "REMAINING_BAIT", 0);

        var messages = WaitForTheCast();

        messages.Should().Equal("Your fishing rod has no bait loaded. Use the fishing rod and target a bait to load some.");
    }

    [Test]
    public void Finishing_at_a_point_without_a_location_asks_for_a_bug_report()
    {
        ReadyToFish(FishingLocationType.Invalid);
        Random.SetSeed(1);
        Cast();

        var messages = WaitForTheCast();

        messages.Should().Equal("Invalid location Id for this fishing point. Please report this with the /bug chat command.");
    }

    [Test]
    public void Finishing_at_a_location_with_no_fish_assigned_asks_for_a_bug_report()
    {
        ReadyToFish((FishingLocationType)9999);
        Random.SetSeed(1);
        Cast();

        var messages = WaitForTheCast();

        messages.Should().Equal("Valid location Id but no fish are assigned to this point. Please report this with the /bug chat command.");
    }

    [Test]
    public void Finishing_with_a_rod_the_system_does_not_know_asks_for_a_bug_report()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(1);
        Cast();
        _resrefs[Rod] = "mystery_rod";

        var messages = WaitForTheCast();

        messages.Should().Equal("Fishing rod is not registered in the system. Please report this with the /bug chat command.");
    }

    [Test]
    public void Finishing_a_cast_with_no_bite_costs_nothing()
    {
        // The deepwoods have no fish of their own, so the default fish is picked; 80% of those casts get no bite.
        ReadyToFish(FishingLocationType.ViscaraDeepwoods);
        var seed = SeedWhere(random => random.Next(3) >= 0 && random.Next(1, 101) <= 80);
        Random.SetSeed(seed);
        Cast();

        var messages = WaitForTheCast();

        messages.Should().Equal("Not even a nibble...");
        _locals.Int(Rod, "REMAINING_BAIT").Should().Be(40);
        _locals.Int(Point, "FISHING_POINT_REMAINING_ATTEMPTS").Should().Be(4);
        _service.Bites.Should().BeEmpty();
    }

    [Test]
    public void Finishing_a_cast_where_the_default_fish_bites_opens_the_fight()
    {
        ReadyToFish(FishingLocationType.ViscaraDeepwoods, rank: 0);
        var seed = SeedWhere(random => random.Next(3) >= 0 && random.Next(1, 101) > 80);
        Random.SetSeed(seed);
        Cast();

        var messages = WaitForTheCast();

        _service.Bites.Should().ContainSingle();
        messages.Should().NotContain("Not even a nibble...");
    }

    [Test]
    public void Finishing_a_bite_hands_the_fight_everything_it_needs_and_leaves_the_bait_and_the_point_to_it()
    {
        // The cavern with a yew rod and trout ball offers only the Nebimonite (level 31).
        ReadyToFish(FishingLocationType.ViscaraCavern);
        Random.SetSeed(SeedWhere(random => random.Next(3) >= 0));
        Cast();

        var messages = WaitForTheCast();

        var bite = _service.Bites.Should().ContainSingle().Subject;
        using (new FluentAssertions.Execution.AssertionScope())
        {
            bite.Player.Should().Be(Player);
            bite.Rod.Should().Be(Rod);
            bite.FishingPoint.Should().Be(Point);
            bite.Fish.Resref.Should().Be("nebimonite");
            bite.RodType.Should().Be(FishingRodType.Yew);
            bite.Rank.Should().Be(CavernSkillRank);
            bite.BaitRemaining.Should().Be(40, "the bite spends the bait, not the service");
            bite.BaitName.Should().Be("Name of " + TroutBall);
            bite.PointAttemptsRemaining.Should().Be(4, "the bite takes the point's attempt, not the service");
            bite.Position.Should().Be(_position);
            bite.Effects.Should().BeSameAs(_service.Effects);
        }

        _locals.Int(Rod, "REMAINING_BAIT").Should().Be(40);
        _locals.Int(Point, "FISHING_POINT_REMAINING_ATTEMPTS").Should().Be(4);
        _engine.CallsTo("CreateItemOnObject").Should().BeEmpty("the fight decides whether the fish is landed");
        messages.Should().BeEmpty();
    }

    [Test]
    public void Finishing_the_last_attempt_still_hands_the_fight_the_point_with_one_attempt_left()
    {
        ReadyToFish(FishingLocationType.ViscaraCavern, attemptsLeft: 1);
        Random.SetSeed(1);
        Cast();

        WaitForTheCast();

        _service.Bites.Should().ContainSingle().Which.PointAttemptsRemaining.Should().Be(1);
        _engine.CallsTo("SetPlotFlag").Should().BeEmpty("the fight exhausts the point");
        _engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }
}
