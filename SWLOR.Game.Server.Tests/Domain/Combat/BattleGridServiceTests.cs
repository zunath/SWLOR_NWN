using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The DM battle grid instance: the defaults, the stored settings, and placing, moving and removing a grid. The engine is
/// a recording fake with real local variables, and its object iterator answers with the anchors the test created.
/// </summary>
[TestFixture]
public class BattleGridServiceTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Area = 700;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<uint> _anchors = new();
    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private BattleGridService _grid = null!;
    private uint _nextId;
    private int _createLimit;

    [SetUp]
    public void SetUp()
    {
        _anchors.Clear();
        _nextId = 9000;
        _createLimit = int.MaxValue;
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        _engine.SetReturnHandler("CreateObject", args =>
        {
            if (Live.Count() >= _createLimit) return Invalid;
            var id = _nextId++;
            _anchors.Add(id);
            return id;
        });
        var cursor = 0;
        _engine.SetReturnHandler("GetFirstObjectInArea", _ =>
        {
            cursor = 0;
            return NextAnchor(ref cursor);
        });
        _engine.SetReturnHandler("GetNextObjectInArea", _ => NextAnchor(ref cursor));
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Placeable);
        _engine.SetReturnHandler("GetTag", _ => BattleGridService.AnchorTag);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _grid = new BattleGridService();
    }

    // The engine iterator skips anchors the code under test has already destroyed.
    private uint NextAnchor(ref int cursor)
    {
        var destroyed = _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToHashSet();
        while (cursor < _anchors.Count)
        {
            var anchor = _anchors[cursor++];
            if (!destroyed.Contains(anchor)) return anchor;
        }

        return Invalid;
    }

    private IReadOnlyList<uint> Destroyed => _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToList();
    private IEnumerable<uint> Live => _anchors.Except(Destroyed);

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    [Test]
    public void AnAreaWithoutSettingsUsesTheDefaults()
    {
        _grid.GetSettings(Area).Should().Be(_grid.DefaultSettings);
        _grid.DefaultSettings.Should().Be(new BattleGridSettings(2f, 2, 2, 0.2f));
        _grid.IsPlaced(Area).Should().BeFalse();
        _grid.GetDrawnSections(Area).Should().Be(0);
    }

    [Test]
    public void PlacingDrawsOneAnchorPerSectionOnASquareCorner()
    {
        var drawn = _grid.Place(Area, new Vector3(13.3f, 7.1f, 1.5f));

        drawn.Should().Be(4);
        _grid.IsPlaced(Area).Should().BeTrue();
        _grid.GetDrawnSections(Area).Should().Be(4);
        _grid.GetCenter(Area).Should().Be(new Vector3(14f, 8f, 1.5f));
        _engine.CountOf("SetName").Should().Be(4);
        _engine.CountOf("SetPlotFlag").Should().Be(4);
        _engine.CountOf("SetUseableFlag").Should().Be(4);
        _engine.CountOf("ApplyEffectToObject").Should().Be(4);
    }

    [Test]
    public void PlacingAgainReplacesTheExistingGrid()
    {
        _grid.Place(Area, Vector3.Zero);
        var first = _anchors.ToArray();

        _grid.Place(Area, new Vector3(20f, 20f, 0f));

        Destroyed.Should().BeEquivalentTo(first);
        Live.Should().HaveCount(4);
    }

    [Test]
    public void AGridTheEngineCannotDrawLeavesTheAreaWithoutOne()
    {
        _grid.Place(Area, Vector3.Zero);
        _createLimit = 0;

        var drawn = _grid.Place(Area, Vector3.Zero);

        drawn.Should().Be(0);
        _grid.IsPlaced(Area).Should().BeFalse();
        _grid.GetDrawnSections(Area).Should().Be(0);
    }

    [Test]
    public void APartlyDrawnGridCountsOnlyWhatWasDrawn()
    {
        _createLimit = 3;

        _grid.Place(Area, Vector3.Zero).Should().Be(3);

        _grid.IsPlaced(Area).Should().BeTrue();
        _grid.GetDrawnSections(Area).Should().Be(3);
    }

    [Test]
    public void ChangedSettingsAreStoredClampedAndRedrawAPlacedGrid()
    {
        _grid.SetSettings(Area, new BattleGridSettings(1f, 3, 1, 0f));

        _grid.GetSettings(Area).Should().Be(new BattleGridSettings(1f, 3, 1, 0f));
        Live.Should().BeEmpty("no grid was placed, so there is nothing to redraw");

        _grid.Place(Area, Vector3.Zero).Should().Be(3);
        _grid.SetSettings(Area, new BattleGridSettings(1f, 4, 2, 0f));

        _grid.GetDrawnSections(Area).Should().Be(8);
        _grid.SetSettings(Area, new BattleGridSettings(100f, 99, 99, 99f));
        _grid.GetSettings(Area).Should().Be(new BattleGridSettings(BattleGridService.MaxSquareSize,
            BattleGridService.MaxSections, BattleGridService.MaxSections, BattleGridService.MaxHeight));
    }

    [Test]
    public void SettingsThatClampBackToTheCurrentOnesDoNotRedraw()
    {
        _grid.Place(Area, Vector3.Zero);
        var created = _engine.CountOf("CreateObject");

        _grid.SetSettings(Area, _grid.DefaultSettings);

        _engine.CountOf("CreateObject").Should().Be(created);
    }

    [Test]
    public void MovingShiftsTheGridByWholeSquares()
    {
        _grid.Move(Area, 1, 1);
        _engine.CountOf("CreateObject").Should().Be(0, "an area without a grid has nothing to move");

        _grid.Place(Area, Vector3.Zero);
        _grid.Move(Area, 3, -2);

        _grid.GetCenter(Area).Should().Be(new Vector3(6f, -4f, 0f));
    }

    [Test]
    public void RemovingDestroysTheAnchorsAndKeepsTheSettings()
    {
        _grid.SetSettings(Area, new BattleGridSettings(1f, 1, 1, 0f));
        _grid.Place(Area, Vector3.Zero);

        _grid.Remove(Area);

        _grid.IsPlaced(Area).Should().BeFalse();
        _grid.GetDrawnSections(Area).Should().Be(0);
        Live.Should().BeEmpty();
        _grid.GetSettings(Area).Should().Be(new BattleGridSettings(1f, 1, 1, 0f));
    }

    [Test]
    public void TheFacadeConstantsMatchTheInstance()
    {
        BattleGrid.AnchorTag.Should().Be(BattleGridService.AnchorTag);
        BattleGrid.SquaresPerSection.Should().Be(BattleGridService.SquaresPerSection);
        BattleGrid.MinSquareSize.Should().Be(BattleGridService.MinSquareSize);
        BattleGrid.MaxSquareSize.Should().Be(BattleGridService.MaxSquareSize);
        BattleGrid.SquareSizeStep.Should().Be(BattleGridService.SquareSizeStep);
        BattleGrid.MinSections.Should().Be(BattleGridService.MinSections);
        BattleGrid.MaxSections.Should().Be(BattleGridService.MaxSections);
        BattleGrid.MinHeight.Should().Be(BattleGridService.MinHeight);
        BattleGrid.MaxHeight.Should().Be(BattleGridService.MaxHeight);
        BattleGrid.HeightStep.Should().Be(BattleGridService.HeightStep);
    }
}
