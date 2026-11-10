using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The venue rating instance: the area rating (authored on the area or taken from the city's Cantina level), the
/// five-minute cache of it, and the Dance Floor point. The engine is a recording fake with real local variables, the
/// database an in-memory one, and the property service a scripted stand-in.
/// </summary>
[TestFixture]
public class PerformanceVenueServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Performer = 1001;
    private const uint Cantina = 2001;
    private const uint Interior = 2002;
    private const uint Open = 2003;
    private const string DanceFloorResref = "dance_floor";

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _property = null!;
    private TestVenue _venue = null!;
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private int _structureLookups;

    [SetUp]
    public void SetUp()
    {
        _areaOf.Clear();
        _resrefs.Clear();
        _structureLookups = 0;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetArea", arguments => _areaOf.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetResRef", arguments => _resrefs.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService();
        var property = ScriptedProxy.Create<IPropertyService>(out _property);
        _property.On("GetPropertyId", arguments => (uint)arguments[0]! == Interior ? "interior" : string.Empty);
        _property.On("GetEffectiveUpgradeLevel", arguments => (string)arguments[0]! == "city" ? 4 : 0);
        _property.On("GetStructureByType", _ =>
        {
            _structureLookups++;
            return new StructureAttribute("Dance Floor", DanceFloorResref, "A dance floor.", true, 0);
        });
        _venue = new TestVenue(_db, property);
        _areaOf[Performer] = Open;
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [TestCase(0, false, 0)]
    [TestCase(3, true, 4)]
    [TestCase(5, true, 5)]
    [TestCase(-2, false, 0)]
    public void Combine_AddsTheDanceFloorPointAndHoldsTheRatingToZeroThroughFive(int area, bool floor, int expected)
    {
        _venue.Combine(area, floor).Should().Be(expected);
    }

    [Test]
    public void AnInvalidAreaRatesZero()
    {
        _venue.GetAreaRating(ObjectInvalid).Should().Be(0);
    }

    [Test]
    public void AnAreaWithAnAuthoredRatingUsesIt_AndHoldsItToFive()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 9);

        _venue.GetAreaRating(Open).Should().Be(5);
    }

    [Test]
    public void APlayerCantinaTakesItsCitysCantinaLevel()
    {
        _areaOf[Performer] = Interior;
        _db.With(new WorldProperty { Id = "interior", PropertyType = PropertyType.Cantina, ParentPropertyId = "building" });
        _db.With(new WorldProperty { Id = "building", PropertyType = PropertyType.Structure, ParentPropertyId = "city" });

        _venue.GetAreaRating(Interior).Should().Be(4);
        _venue.GetBreakdown(Performer).AreaText.Should().Be("Player cantina, city Cantina Lvl 4");
    }

    [Test]
    public void AnAreaThatIsNoPropertyOrNoCantinaRatesZero()
    {
        _venue.GetAreaRating(Open).Should().Be(0, "an area with no property id");

        _venue.ClearCache();
        _db.With(new WorldProperty { Id = "interior", PropertyType = PropertyType.House, ParentPropertyId = "building" });
        _venue.GetAreaRating(Interior).Should().Be(0, "an interior that is not a cantina");

        _venue.ClearCache();
        _db.With(new WorldProperty { Id = "interior", PropertyType = PropertyType.Cantina, ParentPropertyId = string.Empty });
        _venue.GetAreaRating(Interior).Should().Be(0, "a cantina with no building");

        _venue.ClearCache();
        _db.With(new WorldProperty { Id = "interior", PropertyType = PropertyType.Cantina, ParentPropertyId = "building" });
        _venue.GetAreaRating(Interior).Should().Be(0, "a building the database does not hold");

        _venue.ClearCache();
        _db.With(new WorldProperty { Id = "building", PropertyType = PropertyType.Structure, ParentPropertyId = string.Empty });
        _venue.GetAreaRating(Interior).Should().Be(0, "a building with no city");
    }

    [Test]
    public void ARatingIsCachedForFiveMinutes()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 2);
        _venue.GetAreaRating(Cantina).Should().Be(2);

        _engine.SetReturnHandler("GetLocalInt", _ => 3);
        _venue.Now = _venue.Now.AddMinutes(4);
        _venue.GetAreaRating(Cantina).Should().Be(2, "still inside the cache lifetime");

        _venue.Now = _venue.Now.AddMinutes(2);
        _venue.GetAreaRating(Cantina).Should().Be(3, "the cached rating lapsed");
    }

    [Test]
    public void LapsedRatingsAreDroppedAsTheCacheGrows()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 1);
        for (uint area = 5000; area < 5064; area++)
            _venue.GetAreaRating(area);

        _venue.Now = _venue.Now.AddMinutes(6);
        _venue.GetAreaRating(6000).Should().Be(1, "the 64 lapsed entries are dropped as the next area is cached");
    }

    [Test]
    public void ARatingStillInsideItsLifetimeSurvivesTheCleanUp()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 1);
        for (uint area = 5000; area < 5064; area++)
            _venue.GetAreaRating(area);

        _venue.GetAreaRating(6000);
        var reads = _engine.CountOf("GetLocalInt");
        _venue.GetAreaRating(5000);

        _engine.CountOf("GetLocalInt").Should().Be(reads, "a rating still inside its lifetime survives the clean-up");
    }

    [Test]
    public void ADanceFloorWithinReachAddsAPoint_AndTheResrefIsLookedUpOnce()
    {
        _resrefs[7001] = "something_else";
        _resrefs[7002] = DanceFloorResref;
        _venue.Nearby.AddRange(new uint[] { 7001, 7002 });

        _venue.HasDanceFloorNearby(Performer).Should().BeTrue();
        _venue.GetRating(Performer).Should().Be(1);
        _structureLookups.Should().Be(1);
    }

    [Test]
    public void WithoutADanceFloorNearTheRatingIsTheAreasAlone()
    {
        _resrefs[7001] = "something_else";
        _venue.Nearby.Add(7001);

        _venue.HasDanceFloorNearby(Performer).Should().BeFalse();
        _venue.GetBreakdown(Performer).Should().BeEquivalentTo(
            new PerformanceVenueBreakdown(0, 0, "Open area, no venue rating", false));
    }

    [Test]
    public void ABreakdownNamesAnAuthoredCantina()
    {
        _engine.SetReturnHandler("GetLocalInt", _ => 3);
        _venue.Nearby.Add(7002);
        _resrefs[7002] = DanceFloorResref;

        var breakdown = _venue.GetBreakdown(Performer);

        breakdown.AreaText.Should().Be("Cantina");
        breakdown.Total.Should().Be(4);
        breakdown.DanceFloor.Should().BeTrue();
    }

    private sealed class TestVenue : PerformanceVenueService
    {
        public TestVenue(IDBService db, IPropertyService property)
            : base(db, property)
        {
        }

        public DateTime Now { get; set; } = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        public List<uint> Nearby { get; } = new();

        public void ClearCache() => Now = Now.AddMinutes(10);

        protected override DateTime UtcNow() => Now;

        protected override IEnumerable<uint> GetPlaceablesNear(uint performer) => Nearby;
    }
}
