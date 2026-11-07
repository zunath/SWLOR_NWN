using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.IntegrationTests.Migrations;

/// <summary>
/// The real migration runner over records in Redis: older-shape records go in, the server's migrations run in order at the
/// two boot steps, and the versions, the records and a second boot are checked in the database.
/// </summary>
[TestFixture]
public class ServerMigrationTests : IntegrationTestBase
{
    private const int Retired = MigrationService.RetiredServerMigrationVersion;

    private static readonly BaseItem Legacy = BaseItem.Electronics5;

    private void SeedConfiguration(int version)
    {
        Db.Set(new ServerConfiguration { Id = "SWLOR_CONFIG", MigrationVersion = version });
        Db.EndFrame();
    }

    private int StoredVersion()
    {
        Db.EndFrame();
        return Db.Get<ServerConfiguration>("SWLOR_CONFIG").MigrationVersion;
    }

    private static string LegacyEnzyme(string resref, BaseItem baseItem, int model) =>
        GffBuilder.Encode(GffBuilder.Root()
            .ResRef("TemplateResRef", resref)
            .Int("BaseItem", (int)baseItem)
            .Byte("ModelPart1", model)
            .Word("xModelPart1", model)
            .Text("UUID", "item-" + resref));

    private static (int BaseItem, int Model, string Uuid) Read(string data)
    {
        var root = GffReader.Read(Convert.FromBase64String(data)).RootStruct;
        object Field(string label) => root.Fields.Single(field => field.Label == label).Value!;
        return (Convert.ToInt32(Field("BaseItem")), Convert.ToInt32(Field("ModelPart1")), (string)Field("UUID"));
    }

    private IntegrationMigrationService Boot(params IServerMigration[] migrations) => BootWith(migrations);

    /// <summary>Boots with the migrations the server assembly holds.</summary>
    private IntegrationMigrationService BootReal() => BootWith(null);

    private IntegrationMigrationService BootWith(IServerMigration[]? migrations)
    {
        var service = new IntegrationMigrationService(Db, migrations);
        service.AfterDatabaseLoaded();
        service.AfterCacheLoaded();
        return service;
    }

    [Test]
    public void ANewDatabase_StartsAtTheRetiredVersionAndRunsNothingBelowIt()
    {
        var ran = new List<int>();

        Boot(new Stand(Retired - 5, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().BeEmpty("a migration production already ran is never run on a new database");
        StoredVersion().Should().Be(Retired);
    }

    [Test]
    public void PendingMigrations_RunInVersionOrderPerPhaseAndTheVersionAdvancesToTheLast()
    {
        SeedConfiguration(Retired);
        var ran = new List<string>();

        Boot(
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 1, MigrationExecutionType.PostDatabaseLoad, ran),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 4, MigrationExecutionType.PostDatabaseLoad, ran));

        ran.Should().Equal($"database {Retired + 1}", $"database {Retired + 4}", $"cache {Retired + 2}", $"cache {Retired + 3}");
        StoredVersion().Should().Be(Retired + 4);
    }

    [Test]
    public void ASecondBoot_RunsNothingAndKeepsTheVersion()
    {
        SeedConfiguration(Retired);
        var first = new List<int>();
        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, first), new Stand(Retired + 2, MigrationExecutionType.PostDatabaseLoad, first));
        first.Should().HaveCount(2);
        var stored = Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG");

        var second = new List<int>();
        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, second), new Stand(Retired + 2, MigrationExecutionType.PostDatabaseLoad, second));

        second.Should().BeEmpty();
        Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG").Should().Be(stored);
    }

    [Test]
    public void AFailingMigration_StopsTheServerKeepsTheVersionAndTheNextBootRunsItAgain()
    {
        SeedConfiguration(Retired);
        var ran = new List<int>();

        Boot(
            new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, ran),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, ran) { Fails = true },
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().Equal(Retired + 1, Retired + 2);
        Fakes.Administration.CountOf("ShutdownServer").Should().Be(1);
        StoredVersion().Should().Be(Retired, "the version advances only after every pending migration succeeded");

        var rerun = new List<int>();
        Boot(
            new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, rerun),
            new Stand(Retired + 2, MigrationExecutionType.PostCacheLoad, rerun),
            new Stand(Retired + 3, MigrationExecutionType.PostCacheLoad, rerun));

        rerun.Should().Equal(Retired + 1, Retired + 2, Retired + 3);
        StoredVersion().Should().Be(Retired + 3);
    }

    [Test]
    public void ADatabaseOlderThanTheRetiredVersion_IsRefusedAndNothingRuns()
    {
        SeedConfiguration(Retired - 1);
        var ran = new List<int>();

        Boot(new Stand(Retired + 1, MigrationExecutionType.PostCacheLoad, ran));

        ran.Should().BeEmpty();
        Fakes.Administration.CountOf("ShutdownServer").Should().Be(1);
        StoredVersion().Should().Be(Retired - 1);
    }

    [Test]
    public void TheServersOwnMigrations_ConvertOlderShapeStoredItemsAndAreIdempotent()
    {
        var real = typeof(IServerMigration).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => (IServerMigration)Activator.CreateInstance(type)!)
            .ToList();
        var current = real.Max(migration => migration.Version);
        current.Should().BeGreaterThan(Retired);

        SeedConfiguration(Retired);
        Db.Set(new InventoryItem { Id = "bank-enzyme", PlayerId = "p1", StorageId = "GLOBAL_BANK", Name = "Enzyme", Resref = "hydrolase_blue", Quantity = 1, IconResref = "iit_ess5_001", Data = LegacyEnzyme("hydrolase_blue", Legacy, 3) });
        Db.Set(new InventoryItem { Id = "bank-dna", PlayerId = "p1", StorageId = "GLOBAL_BANK", Name = "DNA", Resref = "beast_dna", Quantity = 1, IconResref = "iit_ess5_002", Data = LegacyEnzyme("beast_dna", Legacy, 1) });
        Db.Set(new InventoryItem { Id = "bank-new", PlayerId = "p1", StorageId = "GLOBAL_BANK", Name = "Enzyme", Resref = "lyase_black", Quantity = 1, IconResref = "iit_enzyme_006", Data = LegacyEnzyme("lyase_black", BaseItem.Enzyme, 6) });
        Db.Set(new MarketItem { Id = "market-enzyme", PlayerId = "p1", Name = "Enzyme", Resref = "isomerase_red", IconResref = "iit_ess4_004", IsListed = true, Data = LegacyEnzyme("isomerase_red", BaseItem.Electronics4, 2) });
        Db.Set(new OverflowItem { Id = "overflow-enzyme", PlayerId = "p1", Name = "Enzyme", Resref = "lyase_red", Quantity = 1, Data = LegacyEnzyme("lyase_red", Legacy, 4) });
        Db.EndFrame();

        BootReal();

        StoredVersion().Should().Be(current);
        var converted = Db.Get<InventoryItem>("bank-enzyme");
        Read(converted.Data).Should().Be(((int)BaseItem.Enzyme, 1, "item-hydrolase_blue"), "hydrolase_blue is enzyme color 1");
        converted.IconResref.Should().Be("iit_enzyme_001");
        converted.PlayerId.Should().Be("p1");
        converted.Quantity.Should().Be(1);

        Read(Db.Get<InventoryItem>("bank-dna").Data).BaseItem.Should().Be((int)Legacy, "beast DNA keeps its 2x2 base item");
        Db.Get<InventoryItem>("bank-dna").IconResref.Should().Be("iit_ess5_002");
        Db.Get<InventoryItem>("bank-new").IconResref.Should().Be("iit_enzyme_006");
        var market = Db.Get<MarketItem>("market-enzyme");
        Read(market.Data).Should().Be(((int)BaseItem.Enzyme, 14, "item-isomerase_red"));
        market.IconResref.Should().Be("iit_enzyme_014");
        Read(Db.Get<OverflowItem>("overflow-enzyme").Data).Should().Be(((int)BaseItem.Enzyme, 10, "item-lyase_red"));

        // A second boot finds the version current and changes nothing.
        var snapshot = new[] { "bank-enzyme", "bank-dna", "bank-new" }.Select(id => Db.GetRawJson<InventoryItem>(id)).ToArray();
        var config = Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG");
        BootReal();
        new[] { "bank-enzyme", "bank-dna", "bank-new" }.Select(id => Db.GetRawJson<InventoryItem>(id)).Should().Equal(snapshot);
        Db.GetRawJson<ServerConfiguration>("SWLOR_CONFIG").Should().Be(config);
    }

    private sealed class Stand : IServerMigration
    {
        private readonly List<string>? _phased;
        private readonly List<int>? _versions;

        public Stand(int version, MigrationExecutionType executionType, List<string> ran)
        {
            Version = version;
            ExecutionType = executionType;
            _phased = ran;
        }

        public Stand(int version, MigrationExecutionType executionType, List<int> ran)
        {
            Version = version;
            ExecutionType = executionType;
            _versions = ran;
        }

        public bool Fails { get; init; }

        public int Version { get; }

        public MigrationExecutionType ExecutionType { get; }

        public void Migrate()
        {
            _versions?.Add(Version);
            _phased?.Add($"{(ExecutionType == MigrationExecutionType.PostDatabaseLoad ? "database" : "cache")} {Version}");
            if (Fails)
                throw new InvalidOperationException("migration failed on purpose");
        }
    }
}
