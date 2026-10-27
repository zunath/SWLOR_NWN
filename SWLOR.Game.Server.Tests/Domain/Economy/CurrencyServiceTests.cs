using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// Unit tests of the currency instance: the engine is replaced by a recording fake and the database by an in-memory
/// one, so no server is needed. The refresh event the instance publishes after a change goes to the GUI service, which
/// has no windows open here and so does nothing.
/// </summary>
[TestFixture]
public class CurrencyServiceTests
{
    private const uint PlayerObject = 1001;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private CurrencyService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new CurrencyService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    // ---- the cache

    [Test]
    public void CacheCurrencies_caches_every_currency_except_the_invalid_one()
    {
        _service.CacheCurrencies();

        var expected = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().Where(type => type != CurrencyType.Invalid);
        foreach (var type in expected)
        {
            _service.GetCurrencyDetail(type).Should().NotBeNull($"{type} is cached");
        }
    }

    [Test]
    public void GetCurrencyDetail_returns_the_details_of_the_currency()
    {
        _service.CacheCurrencies();

        var detail = _service.GetCurrencyDetail(CurrencyType.RebuildToken);

        detail.Name.Should().Be("Rebuild Tokens");
    }

    [Test]
    public void GetCurrencyDetail_fails_for_the_invalid_currency_and_before_the_cache_is_filled()
    {
        var before = () => _service.GetCurrencyDetail(CurrencyType.RebuildToken);
        before.Should().Throw<KeyNotFoundException>();

        _service.CacheCurrencies();

        var invalid = () => _service.GetCurrencyDetail(CurrencyType.Invalid);
        invalid.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        _service.CacheCurrencies();

        var other = new CurrencyService(_db);

        var get = () => other.GetCurrencyDetail(CurrencyType.RebuildToken);
        get.Should().Throw<KeyNotFoundException>("state lives on the instance, not in static fields");
    }

    // ---- balances

    [Test]
    public void GetCurrency_is_zero_for_an_unknown_player_and_for_a_currency_never_held()
    {
        _service.GetCurrency(PlayerObject, CurrencyType.RebuildToken).Should().Be(0);

        _db.With(new Player { Id = PlayerId });

        _service.GetCurrency(PlayerObject, CurrencyType.RebuildToken).Should().Be(0);
    }

    [Test]
    public void GetCurrency_returns_the_balance_held()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 7;
        _db.With(player);

        _service.GetCurrency(PlayerObject, CurrencyType.RebuildToken).Should().Be(7);
    }

    [Test]
    public void GiveCurrency_adds_to_the_balance_and_saves_the_player()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 7;
        _db.With(player);

        _service.GiveCurrency(PlayerObject, CurrencyType.RebuildToken, 3);

        player.Currencies[CurrencyType.RebuildToken].Should().Be(10);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
    }

    [Test]
    public void GiveCurrency_starts_a_balance_for_a_currency_never_held()
    {
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.GiveCurrency(PlayerObject, CurrencyType.PerkRefundToken, 2);

        player.Currencies[CurrencyType.PerkRefundToken].Should().Be(2);
    }

    [Test]
    public void GiveCurrency_treats_a_negative_amount_as_its_absolute_value()
    {
        var player = new Player { Id = PlayerId };
        _db.With(player);

        _service.GiveCurrency(PlayerObject, CurrencyType.RebuildToken, -4);

        player.Currencies[CurrencyType.RebuildToken].Should().Be(4);
    }

    [Test]
    public void GiveCurrency_does_nothing_for_an_unknown_player()
    {
        _service.GiveCurrency(PlayerObject, CurrencyType.RebuildToken, 3);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void TakeCurrency_removes_from_the_balance_and_saves_the_player()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 7;
        _db.With(player);

        _service.TakeCurrency(PlayerObject, CurrencyType.RebuildToken, 3);

        player.Currencies[CurrencyType.RebuildToken].Should().Be(4);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);
    }

    [Test]
    public void TakeCurrency_never_takes_the_balance_below_zero()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 2;
        _db.With(player);

        _service.TakeCurrency(PlayerObject, CurrencyType.RebuildToken, 50);

        player.Currencies[CurrencyType.RebuildToken].Should().Be(0);
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void TakeCurrency_treats_a_negative_amount_as_its_absolute_value()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 7;
        _db.With(player);

        _service.TakeCurrency(PlayerObject, CurrencyType.RebuildToken, -3);

        player.Currencies[CurrencyType.RebuildToken].Should().Be(4);
    }

    [Test]
    public void TakeCurrency_saves_nothing_when_the_balance_does_not_change()
    {
        var player = new Player { Id = PlayerId };
        player.Currencies[CurrencyType.RebuildToken] = 0;
        _db.With(player);

        _service.TakeCurrency(PlayerObject, CurrencyType.RebuildToken, 5);
        _service.TakeCurrency(PlayerObject, CurrencyType.PerkRefundToken, 5);

        _db.Saved.Should().BeEmpty();
        player.Currencies[CurrencyType.PerkRefundToken].Should().Be(0, "a currency never held starts at zero");
    }

    [Test]
    public void TakeCurrency_does_nothing_for_an_unknown_player()
    {
        _service.TakeCurrency(PlayerObject, CurrencyType.RebuildToken, 3);

        _db.Saved.Should().BeEmpty();
    }
}
