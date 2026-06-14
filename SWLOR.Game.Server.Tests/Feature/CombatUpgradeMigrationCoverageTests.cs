using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatUpgradeMigrationCoverageTests
{
    [Test]
    public void CombatUpgradeServerMigration_ForcesFullRebuildWithoutTokenFlow()
    {
        var root = FindRepositoryRoot();
        var serverMigrations = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration");
        var previousMigration = File.ReadAllText(Path.Combine(serverMigrations, "_21_SetDefaultOutfitAndMarketLimits.cs"));
        var combatMigration = File.ReadAllText(Path.Combine(serverMigrations, "_22_CombatSystemReplacement.cs"));

        previousMigration.Should().Contain("public int Version => 21;");
        combatMigration.Should().Contain("public int Version => 22;");
        combatMigration.Should().Contain("dbPlayer.RebuildComplete = false;");
        combatMigration.Should().Contain("WeaponBlueprintPerkMigration.CollapsePlayerPerks");
        combatMigration.Should().Contain("DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries");
        combatMigration.Should().Contain("CombatReadinessMigration.ResetCombatReadiness");
        combatMigration.Should().NotContain("RebuildToken");
        combatMigration.Should().NotContain("GrantRebuild");
    }

    [Test]
    public void PlayerMigrations_InvokeLiveObjectCombatUpgradeMigrations()
    {
        var root = FindRepositoryRoot();
        var playerMigrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration");
        var requirementMigration = File.ReadAllText(Path.Combine(playerMigrationRoot, "_13_UpdateItemRequirements.cs"));
        var resistanceMigration = File.ReadAllText(Path.Combine(playerMigrationRoot, "_14_MigrateResistanceItemProperties.cs"));
        var obsoleteItemMigration = File.ReadAllText(Path.Combine(playerMigrationRoot, "_15_RemoveObsoleteCombatInstructionDiscs.cs"));

        requirementMigration.Should().Contain("public override int Version => 13;");
        requirementMigration.Should().Contain("EquipmentRequirementMigration.MigrateObject(player);");
        resistanceMigration.Should().Contain("public override int Version => 14;");
        resistanceMigration.Should().Contain("SerializedItemResistanceMigration.MigrateObject(player);");
        resistanceMigration.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        resistanceMigration.Should().Contain("CombatReadinessMigration.MigratePlayer(player);");
        obsoleteItemMigration.Should().Contain("public override int Version => 15;");
        obsoleteItemMigration.Should().Contain("ObsoleteItemMigration.RemoveObsoleteItemsFromObject(player);");
    }

    [Test]
    public void EquipmentRequirementMigration_CoversStoredItemSurfaces()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_23_UpdateSerializedItemRequirements.cs"));

        migration.Should().Contain("public int Version => 23;");
        migration.Should().Contain("EquipmentRequirementMigration.MigrateSerializedObject");
        AssertMigrationCalls(migration,
            "MigrateInventoryItems();",
            "MigrateMarketItems();",
            "MigrateWorldPropertyCategories();",
            "MigrateWorldProperties();",
            "MigrateResearchJobs();",
            "MigratePlayerOutfits();",
            "MigrateDMCreatures();");
        AssertStoredEntitySurfaces(migration);
    }

    [Test]
    public void DamageResistanceAndDelayMigrations_CoverStoredItemSurfacesIncludingShips()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration");
        var itemPropertyMigration = File.ReadAllText(Path.Combine(migrationRoot, "_31_MigrateResistanceItemProperties.cs"));
        var resistanceKeyMigration = File.ReadAllText(Path.Combine(migrationRoot, "_32_SpaceResistanceTypeIds.cs"));

        itemPropertyMigration.Should().Contain("public int Version => 31;");
        itemPropertyMigration.Should().Contain("SerializedItemResistanceMigration.MigrateSerializedObject");
        itemPropertyMigration.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateSerializedObject");
        AssertStoredEntitySurfaces(itemPropertyMigration);
        AssertShipSurfaces(itemPropertyMigration);

        resistanceKeyMigration.Should().Contain("public int Version => 32;");
        resistanceKeyMigration.Should().Contain("MigrateResistanceDictionaryEntities<Player>");
        resistanceKeyMigration.Should().Contain("MigrateResistanceDictionaryEntities<Beast>");
        resistanceKeyMigration.Should().Contain("MigrateResistanceDictionaryEntities<IncubationJob>");
        resistanceKeyMigration.Should().Contain("SerializedItemResistanceMigration.MigrateSerializedObject");
        AssertStoredEntitySurfaces(resistanceKeyMigration);
        AssertShipSurfaces(resistanceKeyMigration);
    }

    [Test]
    public void ObsoleteBiblePerkMigration_CoversPlayersBeastsStoredItemsDroidsAndShips()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_34_RemoveObsoleteBiblePerks.cs"));
        var obsoleteItemMigration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ObsoleteItemMigration.cs"));

        migration.Should().Contain("public int Version => 34;");
        migration.Should().Contain("MigratePlayers();");
        migration.Should().Contain("MigrateBeasts();");
        migration.Should().Contain("MigrateStoredObsoleteItems();");
        migration.Should().Contain("PlayerRemovedPerks");
        migration.Should().Contain("PlayerTrimmedPerks");
        migration.Should().Contain("BeastRemovedPerks");
        migration.Should().Contain("BeastTrimmedPerks");
        migration.Should().Contain("ObsoleteRecastGroups");
        migration.Should().Contain("RemoveUnlockedPerks(player)");
        migration.Should().Contain("RemoveRecastTimes(player)");
        AssertMigrationCalls(migration,
            "MigrateInventoryItems(ref migratedRecords, ref droidPerksMigrated);",
            "MigrateMarketItems(ref migratedRecords, ref droidPerksMigrated);",
            "MigrateWorldPropertyCategories(ref migratedRecords, ref droidPerksMigrated);",
            "MigrateSerializedField<WorldProperty>",
            "MigrateSerializedField<ResearchJob>",
            "MigrateSerializedField<PlayerOutfit>",
            "MigrateSerializedField<DMCreature>",
            "MigratePlayerShips(ref migratedRecords, ref droidPerksMigrated);");
        AssertShipSurfaces(migration);

        obsoleteItemMigration.Should().Contain("CurrentDroidInstructionMaxLevels");
        obsoleteItemMigration.Should().Contain("RemoveObsoleteItemsFromConstructedDroid");
        obsoleteItemMigration.Should().Contain("SyncDroidInstructionProperties");
        obsoleteItemMigration.Should().Contain("id_concgren3");
        obsoleteItemMigration.Should().Contain("id_tranqshot3");
    }

    [Test]
    public void SharedItemMigrationServices_RecurseThroughEquippedItemsInventoryAndNestedDroidItems()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition");
        var recursiveObjectMigrations = new[]
        {
            "EquipmentRequirementMigration.cs",
            "SerializedItemResistanceMigration.cs",
            "SerializedItemWeaponDamageTypeMigration.cs",
        };

        foreach (var file in recursiveObjectMigrations)
        {
            var source = File.ReadAllText(Path.Combine(migrationRoot, file));
            source.Should().Contain("GetHasInventory(obj)", file);
            source.Should().Contain("GetFirstItemInInventory(obj)", file);
            source.Should().Contain("GetItemInSlot((InventorySlot)index, creature)", file);
        }

        var obsoleteItemMigration = File.ReadAllText(Path.Combine(migrationRoot, "ObsoleteItemMigration.cs"));
        obsoleteItemMigration.Should().Contain("GetHasInventory(obj)");
        obsoleteItemMigration.Should().Contain("GetFirstItemInInventory(obj)");
        obsoleteItemMigration.Should().Contain("GetItemInSlot((InventorySlot)index, obj)");
        obsoleteItemMigration.Should().Contain("droid.EquippedItems");
        obsoleteItemMigration.Should().Contain("droid.Inventory");
    }

    private static void AssertMigrationCalls(string source, params string[] calls)
    {
        foreach (var call in calls)
        {
            source.Should().Contain(call);
        }
    }

    private static void AssertStoredEntitySurfaces(string source)
    {
        foreach (var entityType in new[]
                 {
                     "InventoryItem",
                     "MarketItem",
                     "WorldPropertyCategory",
                     "WorldProperty",
                     "ResearchJob",
                     "PlayerOutfit",
                     "DMCreature",
                 })
        {
            source.Should().Contain(entityType);
        }
    }

    private static void AssertShipSurfaces(string source)
    {
        source.Should().Contain("PlayerShip");
        source.Should().Contain("HighPowerModules");
        source.Should().Contain("LowPowerModules");
        source.Should().Contain("ConfigurationModules");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
