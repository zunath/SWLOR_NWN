using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.BeastMasteryService;
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
    public void CombatUpgradeServerMigration_ForcesFullRebuildAndGrantsRebuildToken()
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
        combatMigration.Should().Contain("RefundBeastPerks(beast, out var perkChanged)");
        combatMigration.Should().NotContain("beast.UnallocatedSP += refund;");
        combatMigration.Should().Contain("Starting consolidated server migration.");
        combatMigration.Should().Contain("Current migration progress:");
        combatMigration.Should().Contain("new MigrationProgress(\"players\", playerCount)");
        combatMigration.Should().Contain("new MigrationProgress(\"beasts\", count)");
        combatMigration.Should().Contain("new MigrationProgress(\"incubation jobs\", count)");
        combatMigration.Should().Contain("SplitDefensesAndResistances(jObject);");
        combatMigration.Should().Contain("NormalizeResistanceDictionary(jObject, nameof(Player.Resistances));");
        combatMigration.Should().Contain("PlayerRemovedPerks");
        combatMigration.Should().Contain("BeastRemovedPerks");
        combatMigration.Should().Contain("GrantCombatUpgradeRebuildToken(dbPlayer);");
        combatMigration.Should().Contain("CurrencyType.RebuildToken");
    }

    [Test]
    public void CombatUpgradeServerMigration_ClearsRecastTimesBeforePlayerDeserialization()
    {
        var clearRecastTimes = typeof(_22_CombatSystemReplacement)
            .GetMethod("ClearRecastTimes", BindingFlags.NonPublic | BindingFlags.Static)!;
        var playerJson = JObject.Parse("""
            {
              "RecastTimes": {
                "SnarlGrowl": "2026-06-30T00:00:00Z",
                "ShieldWall": "2026-06-30T00:01:00Z"
              }
            }
            """);

        ((bool)clearRecastTimes.Invoke(null, new object[] { playerJson })!)
            .Should()
            .BeTrue();
        var recastTimes = (JObject)playerJson[nameof(Player.RecastTimes)]!;

        recastTimes.Properties().Should().BeEmpty();
        playerJson.ToObject<Player>()!.RecastTimes.Should().BeEmpty();
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
        resistanceMigration.Should().Contain("PistolBaseItemMigration.MigratePlayer(player);");
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
        var droidBoostMigration = migration[
            migration.IndexOf("private static class DroidBoostStoredItemMigration", StringComparison.Ordinal)..];
        droidBoostMigration.Should().Contain("wasMigrated = EquipmentRequirementMigration.MigrateObject(obj)");
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
        pistolMigration.Should().Contain("GetItemInSlot(InventorySlot.Arrows, creature)");
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
            "MigrateInventoryItems(progress);",
            "MigrateMarketItems(progress);",
            "MigrateWorldPropertyCategories(categories, progress);",
            "MigrateEntityItems(SearchAll<WorldProperty>()",
            "MigrateEntityItems(researchJobs",
            "MigrateEntityItems(SearchAll<PlayerOutfit>()",
            "MigrateEntityItems(SearchAll<DMCreature>()",
            "MigratePlayerShips(ships, progress);");
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
    public void BeastMigration_RefundsAllBeastPerksAndNormalizesSkillPoints()
    {
        var refundBeastPerks = typeof(_22_CombatSystemReplacement)
            .GetMethod("RefundBeastPerks", BindingFlags.NonPublic | BindingFlags.Static)!;
        var beast = new Beast
        {
            Level = 50,
            UnallocatedSP = 35,
            Perks =
            {
                [PerkType.EnduranceLink] = 3,
                [PerkType.PoisonBreath] = 5
            }
        };

        object[] args = { beast, false };

        ((int)refundBeastPerks.Invoke(null, args)!)
            .Should()
            .Be(15);
        ((bool)args[1]).Should().BeTrue();
        beast.Perks.Should().BeEmpty();
        beast.UnallocatedSP.Should().Be(50);
    }

    [Test]
    public void BeastMigration_RenamesOnlyTheUnmodifiedGoldmaneDefaultName()
    {
        var normalizeDefaultBeastName = typeof(_22_CombatSystemReplacement)
            .GetMethod("NormalizeDefaultBeastName", BindingFlags.NonPublic | BindingFlags.Static)!;
        var legacyDefault = new Beast
        {
            Type = BeastType.GoldmaneSahrak,
            Name = "Goldmane Sahrak"
        };
        var customName = new Beast
        {
            Type = BeastType.GoldmaneSahrak,
            Name = "Sunny"
        };
        var anotherBeast = new Beast
        {
            Type = BeastType.RubybackDrakon,
            Name = "Goldmane Sahrak"
        };

        ((bool)normalizeDefaultBeastName.Invoke(null, new object[] { legacyDefault })!)
            .Should()
            .BeTrue();
        legacyDefault.Name.Should().Be("Goldpelt Sahrak");

        ((bool)normalizeDefaultBeastName.Invoke(null, new object[] { customName })!)
            .Should()
            .BeFalse();
        customName.Name.Should().Be("Sunny");

        ((bool)normalizeDefaultBeastName.Invoke(null, new object[] { anotherBeast })!)
            .Should()
            .BeFalse();
        anotherBeast.Name.Should().Be("Goldmane Sahrak");
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
            (int)DroidStatSubType.Throwing
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
            GetTsvInt(row, 16).Should().Be(GetWeaponGroupRank(stats, 122, 123), $"{templateResref} generator martial rank should match concrete skills");
            GetTsvInt(row, 17).Should().Be(GetWeaponGroupRank(stats, 124, 125, 126), $"{templateResref} generator ranged rank should match concrete skills");
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
        serverMigration.Should().Contain("ClearRecastTimes(jObject)");
        serverMigration.Should().Contain("RemoveUnlockedPerks(dbPlayer)");
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
