#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MigrationDataTests
{
    private static readonly Assembly ServerAssembly = typeof(_22_CombatSystemReplacement).Assembly;

    [Test]
    public void ItemMigrationRunsAfterCachesWhileRawPlayerRepairsRunBeforeCaches()
    {
        var migrations = ServerAssembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IServerMigration).IsAssignableFrom(type))
            .Select(type => (IServerMigration)Activator.CreateInstance(type)!).ToArray();
        migrations.GroupBy(migration => (migration.Version, migration.ExecutionType))
            .Should().OnlyContain(group => group.Count() == 1);
        migrations.Where(migration => migration.Version == 22).Should().HaveCount(2);
        new _22_CombatSystemReplacement().ExecutionType.Should().Be(MigrationExecutionType.PostDatabaseLoad);
        new StoredItemSchemaMigration().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
    }

    [Test]
    public void AFailedLaterPhaseCannotCheckpointAnEarlierHigherVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        var run = type.GetMethod("Run")!;
        run.Invoke(state, new object[] { new _22_CombatSystemReplacement(), (Action<IServerMigration>)(_ => { }) });
        Action fail = () => run.Invoke(state, new object[]
        {
            new StoredItemSchemaMigration(), (Action<IServerMigration>)(_ => throw new InvalidOperationException("failed item"))
        });
        fail.Should().Throw<TargetInvocationException>();
        type.GetProperty("Failed")!.GetValue(state).Should().Be(true);
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(20);

        var executed = false;
        Action retry = () => run.Invoke(state, new object[]
        {
            new StoredItemSchemaMigration(), (Action<IServerMigration>)(_ => executed = true)
        });
        retry.Should().Throw<TargetInvocationException>();
        executed.Should().BeFalse();
    }

    [Test]
    public void SuccessfulPhasesKeepTheHighestCompletedVersion()
    {
        var type = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.ServerMigrationState")!;
        var state = Activator.CreateInstance(type, 20)!;
        foreach (var migration in new IServerMigration[] { new _22_CombatSystemReplacement(), new _21_SetDefaultOutfitAndMarketLimits(), new StoredItemSchemaMigration() })
            type.GetMethod("Run")!.Invoke(state, new object[] { migration, (Action<IServerMigration>)(_ => { }) });
        type.GetProperty("CompletedVersion")!.GetValue(state).Should().Be(22);
    }

    [Test]
    public void RetryingServerPlayerConversionPreservesCurrenciesAndDoesNotRefundTwice()
    {
        var raw = PlayerJson();
        raw["Version"] = 12;
        raw["UnallocatedSP"] = 5;
        raw["Currencies"] = JObject.Parse("""{"RebuildToken":3}""");
        raw["Perks"] = JObject.Parse("""{"HackingBlade":3,"8":2}""");
        var player = MigratePlayer(raw, out var refund)!;
        refund.Should().Be(9, "numeric and named aliases represent the same purchased ranks");
        player.UnallocatedSP.Should().Be(14);
        player.Currencies[CurrencyType.RebuildToken].Should().Be(3, "the bonus token belongs to the player migration");
        player.Version.Should().Be(12, "server conversion must not advance player migrations");
        player.RebuildComplete.Should().BeFalse();

        var saved = JObject.FromObject(player);
        var before = saved.DeepClone();
        var retried = MigratePlayer(saved, out refund)!;
        refund.Should().Be(0);
        JToken.DeepEquals(JObject.FromObject(retried), before).Should().BeTrue();
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 4)]
    [TestCase(4, 7)]
    [TestCase(5, 10)]
    public void LegacyBlueprintRefundMatchesThePurchasedPrice(int rank, int expected)
    {
        var raw = PlayerJson();
        raw["Perks"] = new JObject { ["BladeBlueprints"] = rank };
        var player = MigratePlayer(raw, out var refund)!;
        refund.Should().Be(expected);
        player.UnallocatedSP.Should().Be(expected);
        player.Perks.Should().BeEmpty();
    }

    [Test]
    public void MultipleBlueprintFamiliesRefundEachInvestmentOnce()
    {
        var raw = PlayerJson();
        raw["Perks"] = JObject.Parse("""{"BladeBlueprints":5,"127":5,"TwoHandedBlueprints":5,"WeaponBlueprints":2}""");
        MigratePlayer(raw, out var refund);
        refund.Should().Be(25);
    }

    [TestCase("VibrobladeProficiency", 5, 10)]
    [TestCase("6", 5, 10)]
    [TestCase("Doublehand", 5, 6)]
    [TestCase("1", 5, 6)]
    [TestCase("WeaponFocusVibroblades", 2, 7)]
    [TestCase("SmokeBomb", 3, 8)]
    [TestCase("210", 3, 8)]
    [TestCase("FlurryStyle", 2, 5)]
    [TestCase("236", 2, 5)]
    [TestCase("388", 1, 3)]
    public void RetiredPerksRefundHistoricalPricesBeforeTheirNamesOrIdsAreDiscarded(string key, int rank, int expected)
    {
        var raw = PlayerJson();
        raw["Perks"] = new JObject { [key] = rank };
        var player = MigratePlayer(raw, out var refund)!;
        refund.Should().Be(expected);
        player.UnallocatedSP.Should().Be(expected);
        player.Perks.Should().BeEmpty();
    }

    [Test]
    public void ANewPerkNameDoesNotInheritTheRefundOfItsReusedLegacyId()
    {
        var raw = PlayerJson();
        raw["Perks"] = JObject.Parse("""{"BlastRadius":3}""");
        var player = MigratePlayer(raw, out var refund)!;
        refund.Should().Be(0, "BlastRadius reuses HackingBlade's ID but is a different perk");
        player.Perks[SWLOR.Game.Server.Service.PerkService.PerkType.BlastRadius].Should().Be(3);
    }

    [TestCase("DualWield", 2, 1, 4)]
    [TestCase("RiotBlade", 9, 3, 8)]
    [TestCase("LegSweep", 42, 3, 9)]
    [TestCase("CrossCut", 43, 3, 8)]
    [TestCase("CircleSlash", 48, 3, 9)]
    [TestCase("DoubleStrike", 49, 3, 8)]
    [TestCase("Slam", 64, 3, 8)]
    [TestCase("SpinningWhirl", 65, 3, 9)]
    [TestCase("RapidShot", 66, 2, 8)]
    [TestCase("QuickDraw", 75, 3, 9)]
    [TestCase("DoubleShot", 76, 3, 8)]
    [TestCase("ExplosiveToss", 81, 3, 9)]
    [TestCase("PiercingToss", 82, 3, 8)]
    [TestCase("CripplingShot", 94, 3, 8)]
    [TestCase("ShieldBash", 242, 3, 8)]
    [TestCase("Bulwark", 244, 1, 3)]
    public void PerkNamesReusedAtNewIdsRefundTheHistoricalInvestmentOnce(string name, int legacyId, int rank, int expected)
    {
        var raw = PlayerJson();
        raw["Perks"] = new JObject { [name] = rank, [legacyId.ToString()] = rank };
        raw["UnlockedPerks"] = new JObject { [name] = DateTime.UtcNow, [legacyId.ToString()] = DateTime.UtcNow };
        var player = MigratePlayer(raw, out var refund)!;
        refund.Should().Be(expected);
        player.UnallocatedSP.Should().Be(expected);
        player.Perks.Should().BeEmpty();
        player.UnlockedPerks.Should().BeEmpty();

        var retried = MigratePlayer(JObject.FromObject(player), out refund)!;
        refund.Should().Be(0);
        retried.UnallocatedSP.Should().Be(expected);
    }

    [Test]
    public void NumericResistanceKeysKeepTheirResistanceMeaning()
    {
        var raw = PlayerJson();
        raw["Resistances"] = JObject.Parse("""{"1":-12,"2":15,"3":8,"4":9,"5":10,"6":11,"7":12,"8":13}""");
        raw["Defenses"] = JObject.Parse("""{"1":20,"2":30}""");
        var player = MigratePlayer(raw, out _)!;
        player.Defenses[CombatDamageType.Physical].Should().Be(20);
        player.Defenses[CombatDamageType.Force].Should().Be(30);
        player.Resistances.Should().BeEquivalentTo(new Dictionary<ResistanceType, int>
        {
            [ResistanceType.Fire] = -12, [ResistanceType.Poison] = 15,
            [ResistanceType.Electrical] = 8, [ResistanceType.Ice] = 9,
            [ResistanceType.Mind] = 10, [ResistanceType.Mobility] = 11,
            [ResistanceType.Trauma] = 12, [ResistanceType.Disruption] = 13
        });
    }

    [TestCase("LightsaberUpgradeKit1", RecipeType.ChiroLightsaberUpgradeKit)]
    [TestCase("367", RecipeType.ChiroLightsaberUpgradeKit)]
    [TestCase("SaberstaffUpgradeKit1", RecipeType.ChiroSaberstaffUpgradeKit)]
    [TestCase("368", RecipeType.ChiroSaberstaffUpgradeKit)]
    public void RetiredSaberRecipeUnlocksSurviveWithNameOrNumericKeys(string key, RecipeType replacement)
    {
        var raw = PlayerJson();
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        raw["UnlockedRecipes"] = new JObject { [key] = date };
        raw["CraftedRecipes"] = new JObject { [key] = date };
        var player = MigratePlayer(raw, out _)!;
        player.UnlockedRecipes[replacement].Should().Be(date);
        player.CraftedRecipes[replacement].Should().Be(date);
    }

    [Test]
    public void RetiredBeastEnumNamesAreClearedBeforeDeserialization()
    {
        var raw = JObject.Parse("""{"Level":20,"Perks":{"RetiredUnknownBeastPerk":3}}""");
        Invoke("ServerMigration._22_CombatSystemReplacement", "ClearBeastPerks", raw).Should().Be(true);
        var beast = raw.ToObject<Beast>()!;
        var args = new object[] { beast, false };
        Invoke("ServerMigration._22_CombatSystemReplacement", "RefundBeastPerks", args);
        beast.Perks.Should().BeEmpty();
        beast.UnallocatedSP.Should().Be(20);
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

    private static JObject PlayerJson() => JObject.Parse("""{"UnknownDisplayName":"A quiet traveler","RebuildComplete":true}""");

    private static Player? MigratePlayer(JObject raw, out int refund)
    {
        var args = new object[] { raw, 0 };
        var player = (Player?)typeof(_22_CombatSystemReplacement)
            .GetMethod("MigratePlayerData", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new _22_CombatSystemReplacement(), args);
        refund = (int)args[1];
        return player;
    }

    private static object? Invoke(string type, string method, params object[] args) =>
        ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + type)!
            .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
}
