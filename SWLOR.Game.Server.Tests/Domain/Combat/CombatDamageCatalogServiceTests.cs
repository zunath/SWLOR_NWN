using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using CombatFacade = SWLOR.Game.Server.Service.Combat;
using PlayerEntity = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage type catalog of <c>Combat</c>: the two tables in enum order, filled on first read or by the cache handler,
/// filling in missing defense entries, and the login step that gives a player the defense and resistance entries they
/// lack. The engine is a recording fake, the database an in-memory one and the resistance service a scripted proxy that
/// answers with the resistance rules. The facade members forward to the container's instance.
/// </summary>
[TestFixture]
public class CombatDamageCatalogServiceTests
{
    private const uint Entering = 1001;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _resistanceCalls = null!;
    private CombatDamageCatalogService _catalog = null!;
    private bool _isPlayer;
    private bool _isDm;

    [SetUp]
    public void SetUp()
    {
        _isPlayer = true;
        _isDm = false;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetEnteringObject", _ => Entering);
        _engine.SetReturnHandler("GetIsPC", _ => _isPlayer);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _db = new InMemoryDBService();
        var resistance = ScriptedProxy.Create<IResistanceService>(out _resistanceCalls);
        _resistanceCalls
            .On(nameof(IResistanceService.CreateDefaultResistanceValues), args => ResistanceRules.CreateDefaultResistanceValues((int)args[0]!))
            .On(nameof(IResistanceService.GetAllResistanceTypes), _ => ResistanceRules.EnumerateResistanceTypes().ToList());
        _catalog = new CombatDamageCatalogService(_db, resistance);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    [Test]
    public void LoadingFillsBothTablesInEnumOrderOnce()
    {
        _catalog.LoadDamageTypes();
        _catalog.LoadDamageTypes();

        _catalog.GetAllDamageTypes().Should().Equal(
            CombatDamageType.Physical, CombatDamageType.Force, CombatDamageType.Fire, CombatDamageType.Poison,
            CombatDamageType.Electrical, CombatDamageType.Ice, CombatDamageType.Sonic);
        _catalog.GetDefenseDamageTypes().Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
    }

    [Test]
    public void TheTablesFillOnTheFirstReadAndStayTheSameInstance()
    {
        var defense = _catalog.GetDefenseDamageTypes();
        defense.Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
        _catalog.GetAllDamageTypes().Should().HaveCount(7);

        _catalog.GetDefenseDamageTypes().Should().BeSameAs(defense, "a read never rebuilds the table");
    }

    [Test]
    public void EnsuringDefensesAddsOnlyTheMissingTypes()
    {
        var defenses = new Dictionary<CombatDamageType, int> { [CombatDamageType.Force] = 9 };

        _catalog.EnsureDefenseValues(defenses, 4).Should().BeTrue();
        defenses.Should().Equal(new Dictionary<CombatDamageType, int>
        {
            [CombatDamageType.Force] = 9,
            [CombatDamageType.Physical] = 4,
        });
        _catalog.EnsureDefenseValues(defenses).Should().BeFalse();

        _catalog.Invoking(catalog => catalog.EnsureDefenseValues(null!)).Should().Throw<ArgumentNullException>()
            .WithParameterName("defenses");
    }

    [Test]
    public void APlayerWithoutTablesGetsTheDefaultsAndIsSaved()
    {
        var player = new PlayerEntity { Id = "uuid-1001", Defenses = null!, Resistances = null! };
        _db.With(player);

        _catalog.AddDamageTypeResistances();

        player.Defenses.Should().Equal(CombatRules.CreateDefaultDefenseValues());
        player.Defenses.Keys.Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
        player.Resistances.Should().Equal(ResistanceRules.CreateDefaultResistanceValues());
        _db.Saved.Should().Equal(player);
    }

    [Test]
    public void APlayerMissingSomeEntriesGetsThemAtZero()
    {
        var player = new PlayerEntity
        {
            Id = "uuid-1001",
            Defenses = new Dictionary<CombatDamageType, int> { [CombatDamageType.Physical] = 5 },
            Resistances = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 7 },
        };
        _db.With(player);

        _catalog.AddDamageTypeResistances();

        player.Defenses.Should().Equal(new Dictionary<CombatDamageType, int>
        {
            [CombatDamageType.Physical] = 5,
            [CombatDamageType.Force] = 0,
        });
        player.Resistances[ResistanceType.Fire].Should().Be(7);
        player.Resistances.Keys.Should().BeEquivalentTo(ResistanceRules.EnumerateResistanceTypes());
        player.Resistances.Where(pair => pair.Key != ResistanceType.Fire).Should().OnlyContain(pair => pair.Value == 0);
        _db.Saved.Should().Equal(player);
        _resistanceCalls.Calls.Select(call => call.Member).Should().Equal(nameof(IResistanceService.GetAllResistanceTypes));
    }

    [Test]
    public void APlayerWithEveryEntryIsNotSaved()
    {
        var player = new PlayerEntity { Id = "uuid-1001" };
        _db.With(player);

        _catalog.AddDamageTypeResistances();

        _db.Saved.Should().BeEmpty();
        _db.Reads.Should().Equal("uuid-1001");
    }

    [Test]
    public void NonPlayersDungeonMastersAndUnknownPlayersAreSkipped()
    {
        _isPlayer = false;
        _catalog.AddDamageTypeResistances();

        _isPlayer = true;
        _isDm = true;
        _catalog.AddDamageTypeResistances();

        _db.Reads.Should().BeEmpty();

        _isDm = false;
        _catalog.AddDamageTypeResistances();

        _db.Reads.Should().Equal("uuid-1001");
        _db.Saved.Should().BeEmpty();
        _resistanceCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void TheContainerHoldsTheCatalogAndTheFacadeReadsIt()
    {
        using var container = new FacadeContainer();
        using var installed = container.Install();

        container.Get<ICombatDamageCatalogService>().Should().BeOfType<CombatDamageCatalogService>();
        CombatFacade.GetDefenseDamageTypes().Should().BeSameAs(container.Get<ICombatDamageCatalogService>().GetDefenseDamageTypes());
        CombatFacade.GetAllDamageTypes().Should().HaveCount(7);
    }

    [Test]
    public void TheFacadeForwardsEachMemberToTheCatalog()
    {
        var all = new List<CombatDamageType> { CombatDamageType.Sonic };
        var defense = new List<CombatDamageType> { CombatDamageType.Force };
        var catalog = ScriptedProxy.Create<ICombatDamageCatalogService>(out var calls);
        calls
            .On(nameof(ICombatDamageCatalogService.LoadDamageTypes), _ => null)
            .On(nameof(ICombatDamageCatalogService.AddDamageTypeResistances), _ => null)
            .On(nameof(ICombatDamageCatalogService.GetAllDamageTypes), _ => all)
            .On(nameof(ICombatDamageCatalogService.GetDefenseDamageTypes), _ => defense)
            .On(nameof(ICombatDamageCatalogService.EnsureDefenseValues), _ => true);
        using var container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton(catalog)));
        using var installed = container.Install();
        var defenses = new Dictionary<CombatDamageType, int>();

        CombatFacade.LoadDamageTypes();
        CombatFacade.AddDamageTypeResistances();
        CombatFacade.GetAllDamageTypes().Should().BeSameAs(all);
        CombatFacade.GetDefenseDamageTypes().Should().BeSameAs(defense);
        CombatFacade.EnsureDefenseValues(defenses, 3).Should().BeTrue();

        calls.Calls.Select(call => call.Member).Should().Equal(
            nameof(ICombatDamageCatalogService.LoadDamageTypes),
            nameof(ICombatDamageCatalogService.AddDamageTypeResistances),
            nameof(ICombatDamageCatalogService.GetAllDamageTypes),
            nameof(ICombatDamageCatalogService.GetDefenseDamageTypes),
            nameof(ICombatDamageCatalogService.EnsureDefenseValues));
        calls.Calls[^1].Arguments.Should().Equal(defenses, 3);
    }

    [Test]
    public void DefaultDefensesNeedNoContainer()
    {
        // Entity constructors (Player through CombatRules, Beast and IncubationJob through Combat) run without one.
        CombatFacade.CreateDefaultDefenseValues(2).Should().Equal(CombatRules.CreateDefaultDefenseValues(2));
        new PlayerEntity().Defenses.Keys.Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
    }
}
