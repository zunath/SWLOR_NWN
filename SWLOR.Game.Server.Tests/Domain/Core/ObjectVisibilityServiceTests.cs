using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the object visibility service: the engine fake walks a scripted module of areas and objects, the
/// visibility plugin fake records the overrides, and the database is in memory.
/// </summary>
[TestFixture]
public class ObjectVisibilityServiceTests
{
    private const uint Area1 = 100;
    private const uint Area2 = 101;
    private const uint Door = 200;
    private const uint Lever = 201;
    private const uint Crate = 202;
    private const uint Decoration = 203;
    private const uint Player1 = 300;
    private const uint Invalid = 0x7F000000;
    private const string PlayerId = "player-uuid-1";

    private FakeNWScript _engine = null!;
    private FakeVisibilityPlugin _visibility = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _visibilityScope = null!;
    private InMemoryDBService _db = null!;
    private RecordingVisibilityService _service = null!;
    private readonly Dictionary<uint, string> _visibilityIds = new();
    private readonly HashSet<uint> _hiddenByDefault = new();
    private readonly Dictionary<uint, string> _names = new();
    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dms = new();
    private uint _entering;

    [SetUp]
    public void SetUp()
    {
        _visibilityIds.Clear();
        _hiddenByDefault.Clear();
        _names.Clear();
        _players.Clear();
        _dms.Clear();
        _entering = Player1;
        _players.Add(Player1);

        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _visibility = new FakeVisibilityPlugin();
        _visibilityScope = VisibilityPluginRuntime.Override(_visibility);
        _db = new InMemoryDBService();
        _service = new RecordingVisibilityService(_db);

        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        _engine.SetReturnHandler("GetLocalString", arguments =>
            (string)arguments[1]! == "VISIBILITY_OBJECT_ID" && _visibilityIds.TryGetValue((uint)arguments[0]!, out var id) ? id : string.Empty);
        _engine.SetReturnHandler("GetLocalBool", arguments =>
            (string)arguments[1]! == "VISIBILITY_HIDDEN_DEFAULT" && _hiddenByDefault.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        _engine.SetReturnHandler("GetIsPC", arguments => _players.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDM", arguments => _dms.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetObjectUUID", arguments => (uint)arguments[0]! == Player1 ? PlayerId : string.Empty);
        _engine.SetReturnHandler("GetName", arguments => _names.TryGetValue((uint)arguments[0]!, out var name) ? name : "unnamed");

        // The module: two areas; the door, the lever and the crate carry visibility ids, the decoration does not.
        var objectsByArea = new Dictionary<uint, uint[]>
        {
            [Area1] = new[] { Door, Decoration },
            [Area2] = new[] { Lever, Crate },
        };
        var cursors = new Dictionary<uint, int>();
        _engine.SetReturnSequence("GetFirstArea", Area1);
        _engine.SetReturnSequence("GetNextArea", Area2);
        _engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
        {
            var area = (uint)arguments[0]!;
            cursors[area] = 0;
            return objectsByArea[area][0];
        });
        _engine.SetReturnHandler("GetNextObjectInArea", arguments =>
        {
            var area = (uint)arguments[0]!;
            var next = ++cursors[area];
            return next < objectsByArea[area].Length ? objectsByArea[area][next] : Invalid;
        });

        _visibilityIds[Door] = "door-id";
        _visibilityIds[Lever] = "lever-id";
        _visibilityIds[Crate] = "   ";
        _hiddenByDefault.Add(Door);
        _hiddenByDefault.Add(Decoration);
    }

    [TearDown]
    public void TearDown()
    {
        _visibilityScope.Dispose();
        _engineScope.Dispose();
    }

    private IEnumerable<(uint Player, uint Target, VisibilityType Type)> Overrides() =>
        _visibility.CallsTo("SetVisibilityOverride")
            .Select(call => ((uint)call.Arguments[0]!, (uint)call.Arguments[1]!, (VisibilityType)call.Arguments[2]!));

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        _engine.ClearCalls();

        _ = new ObjectVisibilityService(_db);

        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_player_entering_has_the_default_hidden_objects_with_a_visibility_id_hidden()
    {
        _service.LoadVisibilityObjects();

        _service.LoadPlayerVisibilityObjects();

        Overrides().Should().Equal(new[] { (Player1, Door, VisibilityType.Hidden) },
            "the crate has a blank id and the decoration has none, so neither is tracked");
    }

    [Test]
    public void A_player_entering_gets_the_overrides_it_saved_and_loses_the_objects_no_longer_tracked()
    {
        var saved = new Player(PlayerId);
        saved.ObjectVisibilities["lever-id"] = VisibilityType.Visible;
        saved.ObjectVisibilities["removed-id"] = VisibilityType.Hidden;
        _db.With(saved);
        _service.LoadVisibilityObjects();

        _service.LoadPlayerVisibilityObjects();

        Overrides().Should().Equal(new (uint, uint, VisibilityType)[]
        {
            (Player1, Door, VisibilityType.Hidden),
            (Player1, Lever, VisibilityType.Visible),
        });
        saved.ObjectVisibilities.Should().ContainSingle().Which.Key.Should().Be("lever-id");
        _db.Saved.Should().BeEmpty("the cleaned list is not saved until the player's record is next written");
    }

    [Test]
    public void A_player_entering_is_read_in_reverse_order_of_the_saved_overrides()
    {
        var saved = new Player(PlayerId);
        saved.ObjectVisibilities["door-id"] = VisibilityType.Visible;
        saved.ObjectVisibilities["lever-id"] = VisibilityType.Hidden;
        _db.With(saved);
        _service.LoadVisibilityObjects();

        _service.LoadPlayerVisibilityObjects();

        Overrides().Skip(1).Should().Equal(new (uint, uint, VisibilityType)[]
        {
            (Player1, Lever, VisibilityType.Hidden),
            (Player1, Door, VisibilityType.Visible),
        });
    }

    [Test]
    public void A_dungeon_master_or_a_non_player_entering_changes_nothing()
    {
        _service.LoadVisibilityObjects();
        _dms.Add(Player1);
        _service.LoadPlayerVisibilityObjects();
        _dms.Clear();
        _entering = Door;
        _service.LoadPlayerVisibilityObjects();

        Overrides().Should().BeEmpty();
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Adjusting_an_object_saves_the_players_choice_and_applies_it()
    {
        _visibilityIds[Lever] = "lever-id";

        _service.AdjustVisibility(Player1, Lever, VisibilityType.Hidden);

        _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Player>()
            .Which.ObjectVisibilities.Should().Equal(new Dictionary<string, VisibilityType> { ["lever-id"] = VisibilityType.Hidden });
        Overrides().Should().Equal(new[] { (Player1, Lever, VisibilityType.Hidden) });
    }

    [Test]
    public void Adjusting_an_object_keeps_the_players_other_choices()
    {
        var saved = new Player(PlayerId);
        saved.ObjectVisibilities["door-id"] = VisibilityType.Hidden;
        _db.With(saved);

        _service.AdjustVisibility(Player1, Lever, VisibilityType.Visible);

        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(saved);
        saved.ObjectVisibilities.Should().Equal(new Dictionary<string, VisibilityType>
        {
            ["door-id"] = VisibilityType.Hidden,
            ["lever-id"] = VisibilityType.Visible,
        });
    }

    [Test]
    public void Adjusting_ignores_a_dungeon_master_a_non_player_and_a_player_target()
    {
        _dms.Add(Player1);
        _service.AdjustVisibility(Player1, Lever, VisibilityType.Hidden);
        _dms.Clear();
        _service.AdjustVisibility(Door, Lever, VisibilityType.Hidden);
        _players.Add(Lever);
        _service.AdjustVisibility(Player1, Lever, VisibilityType.Hidden);

        Overrides().Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _service.Logged.Should().BeEmpty();
    }

    [Test]
    public void Adjusting_an_object_without_a_visibility_id_logs_the_mistake_and_changes_nothing()
    {
        _names[Decoration] = "Wall panel";
        _names[Player1] = "Zunath";

        _service.AdjustVisibility(Player1, Decoration, VisibilityType.Hidden);

        _service.Logged.Should().ContainSingle().Which.Should().Be((LogGroup.Error,
            "Wall panel is missing the local variable VISIBILITY_OBJECT_ID. The visibility of this object cannot be modified for player Zunath",
            true));
        Overrides().Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Adjusting_by_id_finds_the_object_the_module_load_indexed()
    {
        _service.LoadVisibilityObjects();

        _service.AdjustVisibilityByObjectId(Player1, "lever-id", VisibilityType.Hidden);

        Overrides().Should().Equal(new[] { (Player1, Lever, VisibilityType.Hidden) });
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void Adjusting_by_an_unknown_id_logs_it()
    {
        _service.LoadVisibilityObjects();

        _service.AdjustVisibilityByObjectId(Player1, "missing-id", VisibilityType.Hidden);

        _service.Logged.Should().ContainSingle().Which.Should().Be((LogGroup.Error,
            "No object matching visibility object Id 'missing-id' can be found. This is likely due to an object with an Id being created after module load.",
            false));
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void A_second_service_instance_knows_nothing_of_the_first_ones_objects()
    {
        _service.LoadVisibilityObjects();
        var other = new RecordingVisibilityService(_db);

        other.AdjustVisibilityByObjectId(Player1, "lever-id", VisibilityType.Hidden);

        other.Logged.Should().ContainSingle();
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void The_log_goes_to_the_static_log_by_default()
    {
        var service = new ObjectVisibilityService(_db);

        // Nothing is registered with the static log in a unit test, so the write is a no-op rather than a failure.
        var write = () => service.AdjustVisibilityByObjectId(Player1, "missing-id", VisibilityType.Hidden);

        write.Should().NotThrow();
    }

    private sealed class RecordingVisibilityService : ObjectVisibilityService
    {
        public RecordingVisibilityService(IDBService db)
            : base(db)
        {
        }

        public List<(LogGroup Group, string Details, bool PrintToConsole)> Logged { get; } = new();

        protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) =>
            Logged.Add((group, details, printToConsole));
    }
}
