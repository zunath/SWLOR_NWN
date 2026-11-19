#nullable enable
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MigrationDataTests
{
    // Master records player migration 25 and server migration 30 as released; NextPlayerVersion (crafting perk conversion and
    // enzyme resize) and NextServerVersion (enzyme resize) shipped on 2026-11-17, and master's post-release cleanup (#3481) moves
    // these constants up. Master's next release owns player migration 27 and server migration 32 (#3481's stubs), so the space
    // revamp takes player 28 and server 33, and the city revamp, launched with space, server 34. Either release can then ship
    // first: one that ran 27 and 32 still runs space's higher versions, and the space build runs master's 27 and 32 first.
    private const int SpacePlayerVersion = 28;
    private const int SpaceServerVersion = 33;
    private const int CityServerVersion = 34;
    private const int MastersNextPlayerVersion = 27;
    private const int MastersNextServerVersion = 32;
    private const int ReleasedPlayerVersion = 25;
    private const int ReleasedServerVersion = 30;
    private const int NextPlayerVersion = ReleasedPlayerVersion + 1;
    private const int NextServerVersion = ReleasedServerVersion + 1;
    private static readonly Assembly ServerAssembly = typeof(SWLOR.Game.Server.Service.Migration).Assembly;

    [Test]
    public void CharacterMigrationsHaveUniqueVersionsAndOneCombinedItemUpdate()
    {
        var migrations = CharacterMigrations.ImplementationTypes
            .Select(type => Activator.CreateInstance(type)!).ToArray();
        migrations.Select(CharacterMigrations.VersionOf).Should().OnlyHaveUniqueItems();
        CharacterMigrations.VersionOf("_17_RemoveWeaponStatOverrides").Should().BeGreaterThan(CharacterMigrations.VersionOf("_16_RetireForceWeaponEnhancements"));
        migrations.Where(migration => CharacterMigrations.VersionOf(migration) >= 22).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { CharacterMigrations.Named("_22_ReviseLootStats"), CharacterMigrations.Named("_23_CorrectEquipmentTypes"),
                CharacterMigrations.Named("_24_CorrectCarapaceBracers"), CharacterMigrations.Named("_25_ConsolidatePelvisAppearance"),
                CharacterMigrations.Named("_26_ConvertCraftingPerksToRanks"), CharacterMigrations.Named("_28_ConvertSpaceItems") });
        // 26 is the crafting revamp's, which shipped on its own; 28 is the space revamp's, above master's next release (27).
        migrations.Where(migration => CharacterMigrations.VersionOf(migration) > ReleasedPlayerVersion).Select(migration => migration.GetType())
            .Should().BeEquivalentTo(new[] { CharacterMigrations.Named("_26_ConvertCraftingPerksToRanks"), CharacterMigrations.Named("_28_ConvertSpaceItems") },
                "each unreleased release receives one combined player-item pass, and the crafting perk conversion ships before the space revamp");
        CharacterMigrations.VersionOf("_26_ConvertCraftingPerksToRanks").Should().Be(NextPlayerVersion);
        CharacterMigrations.VersionOf("_28_ConvertSpaceItems").Should().Be(SpacePlayerVersion).And.BeGreaterThan(MastersNextPlayerVersion);
        CharacterMigrations.LatestVersion.Should().Be(SpacePlayerVersion);
    }

    [Test]
    public void TheCharacterUpgradeRunsEveryMigrationInVersionOrder()
    {
        var registered = CharacterMigrations.Registered.Select(CharacterMigrations.VersionOf).ToArray();
        registered.Should().OnlyHaveUniqueItems();
        registered.Should().BeInAscendingOrder("the upgrade applies migrations oldest first");
        registered.Should().BeEquivalentTo(
            CharacterMigrations.ImplementationTypes.Select(type => CharacterMigrations.VersionOf(Activator.CreateInstance(type)!)),
            "every migration class is registered");
        CharacterMigrations.LatestVersion.Should().Be(registered[^1]);
    }

    [TestCase(112, -12)]
    [TestCase(200, -100)]
    [TestCase(15, 15)]
    public void ResistanceRowsAreDecodedBeforeMigrationMerges(int encoded, int expected)
    {
        Invoke("SerializedItemResistanceMigration", "GetMigrationPropertyValue", ItemPropertyType.Resistance, encoded)
            .Should().Be(expected);
    }

    [Test]
    public void RecipeVariantIdsAreStableAndDistinct()
    {
        var first = Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4800);
        first.Should().Be(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4800));
        first.Should().NotBe(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "source", 4801));
        first.Should().NotBe(Invoke("ServerMigration.StoredItemDataMigration", "GetVariantId", "other", 4800));
        first.Should().Be("source-recipe-4800");
    }

    [Test]
    public void AFailedLaterPhaseCannotCheckpointAnEarlierHigherVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        var run = type.GetMethod("Run")!;
        run.Invoke(state, new object[] { new StandInMigration(22, MigrationExecutionType.PostDatabaseLoad), (Action<IServerMigration>)(_ => { }) });
        var later = new StandInMigration(21, MigrationExecutionType.PostCacheLoad);
        Action fail = () => run.Invoke(state, new object[]
        {
            later, (Action<IServerMigration>)(_ => throw new InvalidOperationException("failed item"))
        });
        fail.Should().Throw<TargetInvocationException>();
        type.GetProperty("Failed")!.GetValue(state).Should().Be(true);
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(20);

        var executed = false;
        Action retry = () => run.Invoke(state, new object[]
        {
            later, (Action<IServerMigration>)(_ => executed = true)
        });
        retry.Should().Throw<TargetInvocationException>();
        executed.Should().BeFalse();
    }

    [Test]
    public void SuccessfulPhasesKeepTheHighestCompletedVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        foreach (var migration in new IServerMigration[]
                 {
                     new StandInMigration(22, MigrationExecutionType.PostDatabaseLoad), new StandInMigration(21, MigrationExecutionType.PostCacheLoad),
                     new StandInMigration(22, MigrationExecutionType.PostCacheLoad),
                 })
            type.GetMethod("Run")!.Invoke(state, new object[] { migration, (Action<IServerMigration>)(_ => { }) });
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(22);
    }

    [Test]
    public void RetiredServerMigrations_StartNewDatabasesAtTheBaselineAndRejectOlderOnes()
    {
        var migration = ServerAssembly.GetType("SWLOR.Game.Server.Service.Migration")!;
        var baseline = (int)migration.GetField("RetiredServerMigrationVersion", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var resolve = migration.GetMethod("ResolveConfiguration", BindingFlags.NonPublic | BindingFlags.Static)!;
        ServerConfiguration Resolve(ServerConfiguration stored) => (ServerConfiguration)resolve.Invoke(null, new object[] { stored! })!;

        baseline.Should().Be(ReleasedServerVersion, "every server migration production has run is retired");
        Resolve(null!).MigrationVersion.Should().Be(baseline, "a new database has nothing for the retired migrations to change");
        Resolve(new ServerConfiguration { MigrationVersion = baseline - 1 }).Should().BeNull("this build no longer has the migrations an older database needs");
        var current = new ServerConfiguration { MigrationVersion = baseline };
        Resolve(current).Should().BeSameAs(current);

        var remaining = ServerAssembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => (IServerMigration)Activator.CreateInstance(type)!).ToArray();
        remaining.Should().OnlyContain(server => server.Version > baseline, "retired migrations are removed, not kept beside the baseline");
        remaining.Select(server => server.GetType()).Should().BeEquivalentTo(
            new[] { typeof(_31_ShrinkEnzymes), typeof(_33_ConvertSpaceItems), typeof(_34_PlayerCityRevamp) },
            $"server migration {NextServerVersion} shipped one combined stored-item pass, the space revamp's stored-item pass sits " +
            $"above master's next release ({MastersNextServerVersion}), and the city revamp's one pass follows it");
        new _31_ShrinkEnzymes().Version.Should().Be(NextServerVersion);
        new _33_ConvertSpaceItems().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
        new _33_ConvertSpaceItems().Version.Should().Be(SpaceServerVersion).And.BeGreaterThan(MastersNextServerVersion);
        new _34_PlayerCityRevamp().ExecutionType.Should().Be(MigrationExecutionType.PostDatabaseLoad);
        new _34_PlayerCityRevamp().Version.Should().Be(CityServerVersion);
    }

    private static object? Invoke(string type, string method, params object[] args) =>
        ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + type)!
            .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);

    private sealed class StandInMigration : IServerMigration
    {
        public StandInMigration(int version, MigrationExecutionType executionType)
        {
            Version = version;
            ExecutionType = executionType;
        }

        public int Version { get; }
        public MigrationExecutionType ExecutionType { get; }
        public void Migrate() { }
    }
}
