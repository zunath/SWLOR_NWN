using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatUpgradeMigrationCoverageTests
{
    private const string PerkTypeEnumMemberPattern = @"^\s*([A-Za-z_]\w*)\s*(?:=\s*-?\d+)?\s*,?\s*(?://.*)?$";

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
        previousMigration.Should().Contain("MigrationExecutionType.PostCacheLoad");
        combatMigration.Should().Contain("public int Version => 22;");
        combatMigration.Should().Contain("MigrationExecutionType.PostDatabaseLoad");
        combatMigration.Should().Contain("dbPlayer.RebuildComplete = false;");
        combatMigration.Should().NotContain("ClearLoggedOutPlayerEffects");
        combatMigration.Should().Contain("WeaponBlueprintPerkMigration.CollapsePlayerPerks");
        combatMigration.Should().Contain("DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries");
        combatMigration.Should().Contain("CombatReadinessMigration.ResetCombatReadiness");
        combatMigration.Should().Contain("StoredItemDataMigration.Migrate();");
        combatMigration.Should().Contain("Starting consolidated server migration.");
        combatMigration.Should().Contain("Current migration progress:");
        combatMigration.Should().Contain("new MigrationProgress(\"players\", playerCount)");
        combatMigration.Should().Contain("new MigrationProgress(\"beasts\", count)");
        combatMigration.Should().Contain("new MigrationProgress(\"incubation jobs\", count)");
        combatMigration.Should().Contain("SplitDefensesAndResistances(jObject);");
        combatMigration.Should().Contain("NormalizeResistanceDictionary(jObject, nameof(Player.Resistances));");
        combatMigration.Should().Contain("PlayerRemovedPerks");
        combatMigration.Should().Contain("BeastRemovedPerks");
        combatMigration.Should().NotContain("RebuildToken");
        combatMigration.Should().NotContain("GrantRebuild");
    }

    [Test]
    public void LoggedOutStatusEffects_RemainProcessLocalRuntimeState()
    {
        var root = FindRepositoryRoot();
        var statusEffectService = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));

        statusEffectService.Should().Contain("private static readonly Dictionary<string, LoggedOutStatusEffects> _loggedOutPlayerEffects = new();");
        statusEffectService.Should().Contain("_loggedOutPlayerEffects[playerId] = new LoggedOutStatusEffects(player, effects, DateTime.UtcNow);");
        statusEffectService.Should().NotContain("ClearLoggedOutPlayerEffects");
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
        obsoleteItemMigration.Should().Contain("PlayerInitialization.ResetFeatsToBaseline(player);");
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
            "StoredItemDataMigration.cs"));
        var serverMigration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_22_CombatSystemReplacement.cs"));

        serverMigration.Should().Contain("StoredItemDataMigration.Migrate();");
        migration.Should().Contain("EquipmentRequirementMigration.MigrateObject(obj)");
        AssertMigrationCalls(migration,
            "MigrateInventoryItems(progress);",
            "MigrateMarketItems(progress);",
            "MigrateWorldPropertyCategories(categories, progress);",
            "MigrateEntityItems(SearchAll<WorldProperty>()",
            "MigrateEntityItems(researchJobs",
            "MigrateEntityItems(SearchAll<PlayerOutfit>()",
            "MigrateEntityItems(SearchAll<DMCreature>()",
            "MigratePlayerShips(ships, progress);");
        AssertStoredEntitySurfaces(migration);
        AssertShipSurfaces(migration);
    }

    [Test]
    public void ServerMigrationsFrom22_ReportCurrentMigrationStartWithoutOverallProgress()
    {
        var root = FindRepositoryRoot();
        var migrationService = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Migration.cs"));

        migrationService.Should().Contain("ConsoleProgressMigrationVersion = 22");
        migrationService.Should().Contain("Starting server migration");
        migrationService.Should().NotContain("migrations 22+ pending");
        migrationService.Should().NotContain("migrations 22+ complete");
    }

    [Test]
    public void SerializedRequirementMigration_ReportsCurrentMigrationRecordProgress()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));

        migration.Should().Contain("Migration #22:");
        migration.Should().Contain("Current migration progress");
        migration.Should().Contain("serialized objects");
        migration.Should().Contain("Current surface");
        migration.Should().Contain("RecordReportStep");
        migration.Should().Contain("PercentReportStep");
        migration.Should().Contain("new MigrationProgress(itemCount, \"records\")");
        migration.Should().Contain("new MigrationProgress(jobCount, \"records\")");
        migration.Should().Contain("new MigrationProgress(totalSerializedObjects, \"serialized objects\")");
        migration.Should().Contain("MigrateDroidBoostSerializedObject(serializedItem, out var migratedData)");
        migration.Should().Contain("false))");
        foreach (var message in new[]
                 {
                     "BeginSection(\"market category records\"",
                     "market category records changed",
                     "BeginSection(\"research job recipe records\"",
                     "research job records written",
                     "BeginSection(\"inventory items\"",
                     "inventory item records changed",
                     "BeginSection(\"market items\"",
                     "market item records changed",
                     "BeginSection(\"world property category storage\"",
                     "category items changed",
                     "\"world property structure items\"",
                     "\"research jobs\"",
                     "\"player outfits\"",
                     "\"DM creatures\"",
                     "{sectionName} records changed",
                     "BeginSection(\"player ships\"",
                     "player ship records changed",
                 })
        {
            migration.Should().Contain(message);
        }
    }

    [Test]
    public void EquipmentRequirementMigration_PrefiltersSerializedObjectsBeforeNwnDeserialization()
    {
        var root = FindRepositoryRoot();
        var migrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "EquipmentRequirementMigration.cs"));

        migrationSource.Should().Contain("CouldContainRequirementMigrationTarget(serializedObject)");
        migrationSource.Should().Contain("ObjectPlugin.Deserialize(serializedObject)");

        var method = Type.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.EquipmentRequirementMigration, SWLOR.Game.Server")!
            .GetMethod("CouldContainRequirementMigrationTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        bool CouldContainRequirementMigrationTarget(string serializedObject)
        {
            return (bool)method.Invoke(null, new object[] { serializedObject })!;
        }

        CouldContainRequirementMigrationTarget("""
        {
          "PropertiesList": {
            "type": "list",
            "value": [
              {
                "PropertyName": { "type": "word", "value": 117 },
                "Subtype": { "type": "word", "value": 0 }
              }
            ]
          }
        }
        """).Should().BeFalse();

        CouldContainRequirementMigrationTarget("""
        {
          "PropertiesList": {
            "type": "list",
            "value": [
              {
                "PropertyName": { "type": "word", "value": 100 },
                "Subtype": { "type": "word", "value": 6 }
              }
            ]
          }
        }
        """).Should().BeTrue();

        CouldContainRequirementMigrationTarget("""
        {
          "PropertiesList": {
            "type": "list",
            "value": [
              {
                "PropertyName": { "type": "word", "value": 131 },
                "Subtype": { "type": "word", "value": 1 }
              }
            ]
          }
        }
        """).Should().BeTrue();

        CouldContainRequirementMigrationTarget("legacy-serialized-object")
            .Should().BeTrue("unknown serialized formats must keep the original deserialization path");
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
        var serverMigration = File.ReadAllText(Path.Combine(migrationRoot, "_22_CombatSystemReplacement.cs"));
        var storedItemMigration = File.ReadAllText(Path.Combine(migrationRoot, "StoredItemDataMigration.cs"));

        storedItemMigration.Should().Contain("SerializedItemResistanceMigration.MigrateObject(obj)");
        storedItemMigration.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(obj)");
        storedItemMigration.Should().Contain("CombatReadinessMigration.MigrateObject(obj)");
        AssertStoredEntitySurfaces(storedItemMigration);
        AssertShipSurfaces(storedItemMigration);

        serverMigration.Should().Contain("NormalizeResistanceDictionary(jObject, nameof(Player.Resistances));");
        serverMigration.Should().Contain("NormalizeResistanceDictionary(jObject, nameof(Beast.ResistancePurities));");
        serverMigration.Should().Contain("NormalizeResistanceDictionary(jObject, nameof(IncubationJob.ResistancePurities));");
    }

    [Test]
    public void WeaponDamageMigration_CollapsesConflictingBlueprintElementalDamageBonuses()
    {
        var root = FindRepositoryRoot();
        var migrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));
        var migrationNotes = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Readmes",
            "CombatUpgradeMigration.md"));

        migrationSource.Should().Contain("BlueprintRecipeIdVariable = \"BLUEPRINT_RECIPE_ID\"");
        migrationSource.Should().Contain("ItemPropertyType.Blueprint");
        migrationSource.Should().Contain("SelectBlueprintDamageEnhancements(selectedEnhancements)");
        migrationSource.Should().Contain("DamageType.IsElementalDamageType()");
        migrationSource.Should().Contain("SWLOR.Game.Server.Service.Random.Next(elementalDamageTypes.Count)");
        migrationSource.Should().Contain("damageEnhancement.DamageType == selectedElementalDamageType");
        migrationNotes.Should().Contain("randomly keeps one elemental type");
    }

    [Test]
    public void ResistanceMigration_MergesLegacyElementalValuesIntoDefaultTargets()
    {
        var moveLegacyElementalDefense = typeof(_22_CombatSystemReplacement)
            .GetMethod("MoveLegacyElementalDefense", BindingFlags.NonPublic | BindingFlags.Static)!;
        var normalizeLegacyElementalResistance = typeof(_22_CombatSystemReplacement)
            .GetMethod("NormalizeLegacyElementalResistance", BindingFlags.NonPublic | BindingFlags.Static)!;

        var defenses = JObject.Parse("""{ "Fire": 14 }""");
        var resistances = JObject.Parse("""{ "Fire": 0 }""");

        ((bool)moveLegacyElementalDefense.Invoke(
            null,
            new object[] { defenses, resistances, CombatDamageType.Fire, ResistanceType.Fire })!)
            .Should()
            .BeTrue();
        resistances["Fire"]!.Value<int>().Should().Be(14);
        defenses.ContainsKey("Fire").Should().BeFalse();

        resistances = JObject.Parse("""{ "Fire": 0, "3": -12 }""");

        ((bool)normalizeLegacyElementalResistance.Invoke(
            null,
            new object[] { resistances, CombatDamageType.Fire, ResistanceType.Fire })!)
            .Should()
            .BeTrue();
        resistances["Fire"]!.Value<int>().Should().Be(-12);
        resistances.ContainsKey("3").Should().BeFalse();
    }

    [Test]
    public void StoredItemMigration_MapsLegacyNumericWeaponCategories()
    {
        var migrationType = typeof(_22_CombatSystemReplacement)
            .Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration")!;
        var tryMapWeaponCategory = migrationType.GetMethod(
            "TryMapWeaponCategory",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(JToken), typeof(MarketCategoryType).MakeByRefType() },
            null)!;

        object[] vibroknifeArgs = { new JValue(2), MarketCategoryType.Invalid };
        ((bool)tryMapWeaponCategory.Invoke(null, vibroknifeArgs)!).Should().BeTrue();
        vibroknifeArgs[1].Should().Be(MarketCategoryType.Vibroknife);

        object[] spearArgs = { new JValue(4), MarketCategoryType.Invalid };
        ((bool)tryMapWeaponCategory.Invoke(null, spearArgs)!).Should().BeTrue();
        spearArgs[1].Should().Be(MarketCategoryType.Spear);
    }

    [Test]
    public void ObsoleteBiblePerkMigration_CoversPlayersBeastsStoredItemsDroidsAndShips()
    {
        var root = FindRepositoryRoot();
        var serverMigration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_22_CombatSystemReplacement.cs"));
        var storedItemMigration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));
        var obsoleteItemMigration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ObsoleteItemMigration.cs"));

        serverMigration.Should().Contain("MigratePlayers();");
        serverMigration.Should().Contain("MigrateBeasts();");
        serverMigration.Should().Contain("StoredItemDataMigration.Migrate();");
        serverMigration.Should().Contain("PlayerRemovedPerks");
        serverMigration.Should().Contain("PlayerTrimmedPerks");
        foreach (var removedLeadershipPerk in new[]
                 {
                     "PerkType.Dedication",
                     "PerkType.SoldiersSpeed",
                     "PerkType.SoldiersStrike",
                     "PerkType.Charge",
                     "PerkType.SoldiersPrecision",
                     "PerkType.ShockingShout",
                     "PerkType.Rejuvenation",
                     "PerkType.FrenziedShout",
                     "PerkType.ShoutRange",
                 })
        {
            serverMigration.Should().Contain(removedLeadershipPerk);
        }

        foreach (var removedBlueprintPerk in new[]
                 {
                     "PerkType.WeaponBlueprints",
                     "PerkType.ArmorBlueprints",
                     "PerkType.AccessoryBlueprints",
                     "PerkType.FurnitureBlueprints",
                     "PerkType.StructureBlueprints",
                     "PerkType.StarshipBlueprints",
                     "PerkType.EnhancementBlueprints",
                     "PerkType.DroidEquipmentBlueprints",
                 })
        {
            serverMigration.Should().Contain(removedBlueprintPerk);
        }

        serverMigration.Should().Contain("BeastRemovedPerks");
        serverMigration.Should().Contain("BeastTrimmedPerks");
        serverMigration.Should().Contain("ObsoleteRecastGroups");
        serverMigration.Should().Contain("RemoveUnlockedPerks(dbPlayer)");
        serverMigration.Should().Contain("RemoveRecastTimes(dbPlayer)");
        AssertMigrationCalls(storedItemMigration,
            "MigrateInventoryItems(progress);",
            "MigrateMarketItems(progress);",
            "MigrateWorldPropertyCategories(categories, progress);",
            "MigrateEntityItems(SearchAll<WorldProperty>()",
            "MigrateEntityItems(researchJobs",
            "MigrateEntityItems(SearchAll<PlayerOutfit>()",
            "MigrateEntityItems(SearchAll<DMCreature>()",
            "MigratePlayerShips(ships, progress);");
        storedItemMigration.Should().Contain("ObsoleteItemMigration.RemoveObsoleteItemsFromObject");
        AssertShipSurfaces(storedItemMigration);

        obsoleteItemMigration.Should().Contain("CurrentDroidInstructionMaxLevels");
        obsoleteItemMigration.Should().Contain("RemoveObsoleteItemsFromConstructedDroid");
        obsoleteItemMigration.Should().Contain("SyncDroidInstructionProperties");
        obsoleteItemMigration.Should().Contain("id_concgren3");
        obsoleteItemMigration.Should().Contain("id_tranqshot3");
    }

    [Test]
    public void RemovedPerkMigration_CoversEveryPerkWithoutCurrentDefinition()
    {
        var root = FindRepositoryRoot();
        var currentPerks = GetCurrentPerkDefinitions(root);
        var cleanupPerks = GetMigrationCleanupPerks(root);
        var enumPerks = GetPerkTypeEnumNames(root);

        var missingCleanup = enumPerks
            .Except(currentPerks)
            .Except(cleanupPerks)
            .OrderBy(perk => perk)
            .ToList();

        missingCleanup.Should().BeEmpty(
            "persisted perk keys without current definitions must be removed or trimmed by the consolidated migration");
    }

    [Test]
    public void RemovedPerkCoverage_ReadsExplicitAndImplicitEnumMembers()
    {
        const string source = """
            Invalid = 0,
            ExplicitPerk = 42,
            ImplicitPerk,
            FinalImplicit
            """;

        Regex.Matches(source, PerkTypeEnumMemberPattern, RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .Should()
            .Equal("Invalid", "ExplicitPerk", "ImplicitPerk", "FinalImplicit");
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
            "CombatReadinessMigration.cs",
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

        var combatReadinessMigration = File.ReadAllText(Path.Combine(migrationRoot, "CombatReadinessMigration.cs"));
        combatReadinessMigration.Should().Contain("droid.EquippedItems");
        combatReadinessMigration.Should().Contain("droid.Inventory");
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

    private static HashSet<string> GetPerkTypeEnumNames(DirectoryInfo root)
    {
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PerkService",
            "PerkType.cs"));

        return Regex.Matches(source, PerkTypeEnumMemberPattern, RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .Where(perk => perk != "Invalid")
            .ToHashSet();
    }

    private static HashSet<string> GetCurrentPerkDefinitions(DirectoryInfo root)
    {
        var perkDefinitionRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "PerkDefinition");
        var currentPerks = new HashSet<string>();
        foreach (var path in Directory.EnumerateFiles(perkDefinitionRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            foreach (Match match in Regex.Matches(
                         source,
                         @"\.Create\(\s*PerkCategoryType\.\w+\s*,\s*PerkType\.(\w+)"))
            {
                currentPerks.Add(match.Groups[1].Value);
            }

            foreach (Match match in Regex.Matches(
                         source,
                         @"Create\w*Perk\(\s*PerkType\.(\w+)"))
            {
                currentPerks.Add(match.Groups[1].Value);
            }
        }

        return currentPerks;
    }

    private static HashSet<string> GetMigrationCleanupPerks(DirectoryInfo root)
    {
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_22_CombatSystemReplacement.cs"));

        var cleanupPerks = new HashSet<string>();
        foreach (var dictionaryName in new[]
                 {
                     "PlayerRemovedPerks",
                     "PlayerTrimmedPerks",
                     "BeastRemovedPerks",
                     "BeastTrimmedPerks",
                 })
        {
            var dictionary = Regex.Match(
                source,
                $@"{dictionaryName}\s*=\s*new\(\)\s*{{(?<body>.*?)^\s*}};",
                RegexOptions.Singleline | RegexOptions.Multiline);
            dictionary.Success.Should().BeTrue($"{dictionaryName} must remain discoverable by migration coverage tests");

            foreach (Match match in Regex.Matches(dictionary.Groups["body"].Value, @"PerkType\.(\w+)"))
            {
                cleanupPerks.Add(match.Groups[1].Value);
            }
        }

        return cleanupPerks;
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
