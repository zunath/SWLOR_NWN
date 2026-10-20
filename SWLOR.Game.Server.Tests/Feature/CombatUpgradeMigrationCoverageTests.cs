using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatUpgradeMigrationCoverageTests
{
    private const string PerkTypeEnumMemberPattern = @"^\s*([A-Za-z_]\w*)\s*(?:=\s*-?\d+)?\s*,?\s*(?://.*)?$";

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
    public void CharacterMigrations_RunTheSavedDataCombatUpgradeMigrations()
    {
        var root = FindRepositoryRoot();
        var characterMigrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "CharacterMigration");
        var requirementMigration = File.ReadAllText(Path.Combine(characterMigrationRoot, "_13_UpdateItemRequirements.cs"));
        var combatUpgradeMigrations = File.ReadAllText(Path.Combine(characterMigrationRoot, "_14_15_CombatUpgradeMigrations.cs"));

        CharacterMigrations.VersionOf("_13_UpdateItemRequirements").Should().Be(13);
        requirementMigration.Should().Contain("SavedEquipmentRequirementMigration.MigrateCharacter(character);");
        CharacterMigrations.VersionOf("_14_MigrateResistanceItemProperties").Should().Be(14);
        combatUpgradeMigrations.Should().Contain("SavedItemResistanceMigration.MigrateCharacter(character);");
        combatUpgradeMigrations.Should().Contain("SavedWeaponDamageTypeMigration.MigrateCharacter(character);");
        combatUpgradeMigrations.Should().Contain("SavedCombatUpgradeItemMigrations.MigrateCombatReadiness(character);");
        combatUpgradeMigrations.Should().Contain("SavedCombatUpgradeItemMigrations.MigratePistols(character);");
        combatUpgradeMigrations.Should().Contain("SavedCombatUpgradeItemMigrations.MigrateAccuracy(character);");
        CharacterMigrations.VersionOf("_15_RemoveObsoleteCombatInstructionDiscs").Should().Be(15);
        combatUpgradeMigrations.Should().Contain("SavedObsoleteItemMigration.MigrateCharacter(character);");
        combatUpgradeMigrations.Should().Contain("CharacterRecalculation.ResetFeatsToBaseline(character);");
    }

    /// <summary>
    /// Guards player, database, ship, and nested droid entry points for pistol and ammunition conversion.
    /// </summary>
    [Test]
    public void PistolBaseItemMigration_CoversPlayersAndStoredItemSurfaces()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition");
        var pistolMigration = File.ReadAllText(Path.Combine(
            migrationRoot,
            "PistolBaseItemMigration.cs"));
        var storedMigration = File.ReadAllText(Path.Combine(
            migrationRoot,
            "ServerMigration",
            "StoredItemDataMigration.cs"));

        storedMigration.Should().Contain("PistolBaseItemMigration.MigrateStoredObject(obj)");
        pistolMigration.Should().Contain("PistolBaseItemCompatibility.Normalize(obj)");
        pistolMigration.Should().Contain("GetEquippedItem(creature, InventorySlot.Arrows)");
        pistolMigration.Should().Contain("CreaturePlugin.RunUnequip(creature, legacyAmmo)");
        pistolMigration.Should().Contain("CreaturePlugin.RunEquip(creature, legacyAmmo, InventorySlot.Bullets)");
        pistolMigration.Should().Contain("ConstructedDroidVariable");
        pistolMigration.Should().Contain("droid.SerializedCPU");
        pistolMigration.Should().Contain("droid.SerializedHead");
        pistolMigration.Should().Contain("droid.SerializedBody");
        pistolMigration.Should().Contain("droid.SerializedArms");
        pistolMigration.Should().Contain("droid.SerializedLegs");
        pistolMigration.Should().Contain("droid.EquippedItems");
        pistolMigration.Should().Contain("droid.Inventory");
        pistolMigration.Should().Contain("MoveEquippedItemToDroidInventory(droid, existingBulletAmmo)");

        AssertMigrationCalls(storedMigration,
            "MigrateInventoryItems(progress, disposal);",
            "MigrateMarketItems(progress, disposal);",
            "MigrateWorldPropertyCategories(categories, progress, disposal);",
            "MigrateEntityItems(SearchAll<WorldProperty>()",
            "MigrateEntityItems(researchJobs",
            "MigrateEntityItems(SearchAll<PlayerOutfit>()",
            "MigrateEntityItems(SearchAll<DMCreature>()",
            "MigratePlayerShips(ships, progress, disposal);");
        AssertStoredEntitySurfaces(storedMigration);
        AssertShipSurfaces(storedMigration);
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
    public void WeaponDamageMigration_CollapsesConflictingBlueprintElementalDamageBonuses()
    {
        var root = FindRepositoryRoot();
        var migrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        migrationSource.Should().Contain("BlueprintRecipeIdVariable = \"BLUEPRINT_RECIPE_ID\"");
        migrationSource.Should().Contain("ItemPropertyType.Blueprint");
        migrationSource.Should().Contain("SelectBlueprintDamageEnhancements(selectedEnhancements)");
        migrationSource.Should().Contain("DamageType.IsElementalDamageType()");
        migrationSource.Should().Contain("SWLOR.Game.Server.Service.Random.Next(elementalDamageTypes.Count)");
        migrationSource.Should().Contain("damageEnhancement.DamageType == selectedElementalDamageType");
    }

    [Test]
    public void DroidCpuTemplates_UseArmorAndConcreteWeaponSkillStats()
    {
        var root = FindRepositoryRoot();
        var cpuFiles = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "d_*cpu*.uti.json")
            .OrderBy(x => x)
            .ToArray();
        var legacyWeaponGroups = new[]
        {
            12,
            13,
            14,
            15
        };
        var concreteWeaponSkills = new[]
        {
            (int)DroidStatSubType.Vibroblade,
            (int)DroidStatSubType.Vibroknife,
            (int)DroidStatSubType.Lightsaber,
            (int)DroidStatSubType.HeavyVibroblade,
            (int)DroidStatSubType.Spear,
            (int)DroidStatSubType.TwinBlade,
            (int)DroidStatSubType.Saberstaff,
            (int)DroidStatSubType.Katar,
            (int)DroidStatSubType.Staff,
            (int)DroidStatSubType.Pistol,
            (int)DroidStatSubType.Rifle,
            (int)DroidStatSubType.Throwing,
            (int)DroidStatSubType.Unarmed,
            (int)DroidStatSubType.TwinPistol
        };

        cpuFiles.Should().NotBeEmpty("droid CPU item templates are the live crafted CPU data");
        var generatorCpuRows = File.ReadLines(Path.Combine(
                root.FullName,
                "SWLOR.CLI",
                "InputFiles",
                "droid_item_list.tsv"))
            .Select(line => line.Split('\t'))
            .Where(row => row.Length > 2 && row[2].Equals("CPU", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(row => row[1], row => row, StringComparer.OrdinalIgnoreCase);
        generatorCpuRows.Should().HaveCount(cpuFiles.Length, "the droid item generator should be able to reproduce every live CPU template");

        foreach (var file in cpuFiles)
        {
            var templateName = Path.GetFileName(file);
            var template = JObject.Parse(File.ReadAllText(file));
            var templateResref = template["TemplateResRef"]!["value"]!.Value<string>()!;
            var stats = template["PropertiesList"]!["value"]!
                .Children<JObject>()
                .Where(prop => GetJsonFieldValue(prop, "PropertyName") == (int)ItemPropertyType.DroidStat)
                .GroupBy(prop => GetJsonFieldValue(prop, "Subtype"))
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(prop => GetJsonFieldValue(prop, "CostValue")).ToArray());
            var tier = stats[(int)DroidStatSubType.Tier].Single();

            stats.Should().ContainKey((int)DroidStatSubType.Armor, $"{templateName} should grant droids armor equipment rank");
            stats[(int)DroidStatSubType.Armor].Should().ContainSingle()
                .Which.Should().Be(GetTierArmorSkillRank(tier), $"{templateName} armor rank should follow CPU tier");
            stats.Keys.Should().Contain(key => concreteWeaponSkills.Contains(key), $"{templateName} should grant concrete weapon skill ranks");

            foreach (var legacyWeaponGroup in legacyWeaponGroups)
            {
                stats.Should().NotContainKey(legacyWeaponGroup, $"{templateName} should not use legacy droid weapon group stat IDs");
            }

            generatorCpuRows.Should().ContainKey(templateResref, $"{templateName} should be reproducible from the droid item generator input");
            var row = generatorCpuRows[templateResref];
            row.Length.Should().BeGreaterThanOrEqualTo(18, $"{templateResref} should define every CPU generator column");
            row[0].Should().Be(template["LocalizedName"]!["value"]!["0"]!.Value<string>());
            GetTsvInt(row, 3).Should().Be(tier, $"{templateResref} generator tier should match the template");
            GetTsvInt(row, 4).Should().Be(stats[(int)DroidStatSubType.AISlots].Single(), $"{templateResref} generator AI slots should match the template");
            GetTsvInt(row, 6).Should().Be(stats[(int)DroidStatSubType.HP].Single(), $"{templateResref} generator HP should match the template");
            GetTsvInt(row, 7).Should().Be(stats[(int)DroidStatSubType.STM].Single(), $"{templateResref} generator STM should match the template");
            GetTsvInt(row, 8).Should().Be(stats[(int)DroidStatSubType.MGT].Single(), $"{templateResref} generator MGT should match the template");
            GetTsvInt(row, 9).Should().Be(stats[(int)DroidStatSubType.PER].Single(), $"{templateResref} generator PER should match the template");
            GetTsvInt(row, 10).Should().Be(stats[(int)DroidStatSubType.VIT].Single(), $"{templateResref} generator VIT should match the template");
            GetTsvInt(row, 11).Should().Be(stats[(int)DroidStatSubType.WIL].Single(), $"{templateResref} generator WIL should match the template");
            GetTsvInt(row, 12).Should().Be(stats[(int)DroidStatSubType.AGI].Single(), $"{templateResref} generator AGI should match the template");
            GetTsvInt(row, 13).Should().Be(stats[(int)DroidStatSubType.SOC].Single(), $"{templateResref} generator SOC should match the template");
            GetTsvInt(row, 14).Should().Be(GetWeaponGroupRank(stats, 115, 116, 117), $"{templateResref} generator one-handed rank should match concrete skills");
            GetTsvInt(row, 15).Should().Be(GetWeaponGroupRank(stats, 118, 119, 120, 121), $"{templateResref} generator two-handed rank should match concrete skills");
            GetTsvInt(row, 16).Should().Be(GetWeaponGroupRank(stats, 122, 123, 128), $"{templateResref} generator martial rank should match concrete skills");
            GetTsvInt(row, 17).Should().Be(GetWeaponGroupRank(stats, 124, 125, 126, 129), $"{templateResref} generator ranged rank should match concrete skills");
            if (GetTsvInt(row, 16) > 0)
                stats.Should().ContainKey((int)DroidStatSubType.Unarmed, $"{templateResref} martial CPUs should grant Unarmed like Katar and Staff");
            if (GetTsvInt(row, 17) > 0)
                stats.Should().ContainKey((int)DroidStatSubType.TwinPistol, $"{templateResref} ranged CPUs should grant Twin Pistol like Pistol, Rifle, and Throwing");
        }

        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));
        migration.Should().Contain("DroidStatSubType.Armor");
        migration.Should().Contain("DroidPartItemPropertySubType.CPU");
        migration.Should().Contain("Droid.DroidControlItemResref");
        migration.Should().Contain("GetTierArmorSkillRank");
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

    private static int GetJsonFieldValue(JObject obj, string field)
    {
        return obj[field]?["value"]?.Value<int>() ?? 0;
    }

    private static int GetTierArmorSkillRank(int tier)
    {
        return tier switch
        {
            1 => 5,
            2 => 15,
            3 => 25,
            4 => 35,
            5 => 45,
            _ => 0
        };
    }

    private static int GetTsvInt(IReadOnlyList<string> row, int index)
    {
        return string.IsNullOrWhiteSpace(row[index])
            ? 0
            : int.Parse(row[index], CultureInfo.InvariantCulture);
    }

    private static int GetWeaponGroupRank(
        IReadOnlyDictionary<int, int[]> stats,
        params int[] subTypes)
    {
        var ranks = subTypes
            .Select(subType => stats.TryGetValue(subType, out var values) ? values.Single() : 0)
            .Where(rank => rank > 0)
            .Distinct()
            .ToArray();

        if (ranks.Length > 1)
            Assert.Fail($"Concrete droid weapon skill ranks differ within a generator group: {string.Join(", ", ranks)}.");

        return ranks.Length == 0
            ? 0
            : ranks[0];
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
