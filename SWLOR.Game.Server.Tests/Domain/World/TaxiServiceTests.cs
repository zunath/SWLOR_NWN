using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.TaxiService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the taxi instance: the engine is replaced by a recording fake and the database by an in-memory one,
/// so no server is needed.
/// </summary>
[TestFixture]
public class TaxiServiceTests
{
    private const uint PlayerObject = 1001;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private TaxiService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new TaxiService(_db);
        _service.LoadTaxiDestinations();
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    private static IEnumerable<TaxiDestinationType> AllTypes() => Enum.GetValues(typeof(TaxiDestinationType)).Cast<TaxiDestinationType>();

    // ---- the cache

    [Test]
    public void LoadTaxiDestinations_groups_every_destination_by_its_region()
    {
        var regions = AllTypes().GroupBy(type => type.GetAttribute<TaxiDestinationType, TaxiDestinationAttribute>().RegionId).ToList();

        foreach (var region in regions)
        {
            _service.GetDestinationsByRegionId(region.Key).Keys.Should().BeEquivalentTo(region, $"region {region.Key}");
        }
    }

    [Test]
    public void GetDestinationsByRegionId_returns_the_attributes_of_the_destinations()
    {
        var veles = _service.GetDestinationsByRegionId(1);

        veles[TaxiDestinationType.VelesEntrance].Name.Should().Be("Veles Entrance");
        veles.Values.Should().OnlyContain(detail => detail.RegionId == 1);
    }

    [Test]
    public void GetDestinationsByRegionId_fails_for_an_unknown_region()
    {
        var get = () => _service.GetDestinationsByRegionId(-12345);

        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetDestinationsByRegionId_hands_out_a_copy()
    {
        _service.GetDestinationsByRegionId(1).Clear();

        _service.GetDestinationsByRegionId(1).Should().NotBeEmpty();
    }

    [Test]
    public void LoadTaxiDestinations_can_run_again_without_changing_the_cache()
    {
        var before = _service.GetDestinationsByRegionId(1).Keys.ToList();

        _service.LoadTaxiDestinations();

        _service.GetDestinationsByRegionId(1).Keys.Should().BeEquivalentTo(before);
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        var other = new TaxiService(_db);

        var get = () => other.GetDestinationsByRegionId(1);

        get.Should().Throw<KeyNotFoundException>("state lives on the instance, not in static fields");
    }

    // ---- registering

    [Test]
    public void RegisterTaxiDestination_adds_the_destination_to_its_region_and_says_so()
    {
        var player = new Player { Id = PlayerEngine.PlayerId };
        _db.With(player);

        _service.RegisterTaxiDestination(PlayerObject, TaxiDestinationType.VelesEntrance);

        player.TaxiDestinations[1].Should().Equal(TaxiDestinationType.VelesEntrance);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
        _engine.Messages().Should().Equal("'Veles Entrance' registered into taxi destinations!");
    }

    [Test]
    public void RegisterTaxiDestination_keeps_the_destinations_the_player_already_had()
    {
        var player = new Player { Id = PlayerEngine.PlayerId };
        player.TaxiDestinations[1] = new List<TaxiDestinationType> { TaxiDestinationType.VelesMarket };
        _db.With(player);

        _service.RegisterTaxiDestination(PlayerObject, TaxiDestinationType.VelesEntrance);

        player.TaxiDestinations[1].Should().Equal(TaxiDestinationType.VelesMarket, TaxiDestinationType.VelesEntrance);
    }

    [Test]
    public void RegisterTaxiDestination_refuses_a_destination_the_player_already_registered()
    {
        var player = new Player { Id = PlayerEngine.PlayerId };
        player.TaxiDestinations[1] = new List<TaxiDestinationType> { TaxiDestinationType.VelesEntrance };
        _db.With(player);

        _service.RegisterTaxiDestination(PlayerObject, TaxiDestinationType.VelesEntrance);

        player.TaxiDestinations[1].Should().Equal(TaxiDestinationType.VelesEntrance);
        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().Equal("You have already registered this location.");
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void RegisterTaxiDestination_refuses_anyone_who_is_not_a_player(bool isPlayer, bool isDM)
    {
        _engine.Engine.SetReturn("GetIsPC", isPlayer);
        _engine.Engine.SetReturn("GetIsDM", isDM);
        _db.With(new Player { Id = PlayerEngine.PlayerId });

        _service.RegisterTaxiDestination(PlayerObject, TaxiDestinationType.VelesEntrance);

        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().Equal("Only players may register taxi destinations.");
    }

    [Test]
    public void RegisterTaxiDestination_fails_for_a_destination_that_is_not_cached_and_saves_nothing()
    {
        _db.With(new Player { Id = PlayerEngine.PlayerId });
        var other = new TaxiService(_db);

        var register = () => other.RegisterTaxiDestination(PlayerObject, TaxiDestinationType.VelesEntrance);

        register.Should().Throw<KeyNotFoundException>();
        _db.Saved.Should().BeEmpty();
    }
}
