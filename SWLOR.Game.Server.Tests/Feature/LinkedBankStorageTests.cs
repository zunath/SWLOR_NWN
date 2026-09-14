using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class LinkedBankStorageTests
{
    [Test]
    public void BankService_UsesCanonicalStorageIdAndCap()
    {
        Bank.GetItemCountText(5).Should().Be("5 / 120 Items");
        Bank.GetStoragePercentage(240).Should().Be(1f);

        var bankType = typeof(Bank);
        var storageId = bankType.GetField("StorageId", BindingFlags.NonPublic | BindingFlags.Static);
        var maxItems = bankType.GetField("MaxItems", BindingFlags.NonPublic | BindingFlags.Static);

        storageId.Should().NotBeNull();
        storageId!.IsLiteral.Should().BeTrue();
        storageId.GetRawConstantValue().Should().Be("GLOBAL_BANK");
        maxItems.Should().NotBeNull();
        maxItems!.IsLiteral.Should().BeTrue();
        maxItems.GetRawConstantValue().Should().Be(120);
        bankType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Should()
            .BeEmpty();
    }

    [Test]
    public void BankService_OwnsBankOperations()
    {
        GetPublicBankMethod(nameof(Bank.GetItemCount), typeof(string))
            .ReturnType
            .Should()
            .Be(typeof(long));
        GetPublicBankMethod(nameof(Bank.SearchItems), typeof(string), typeof(string))
            .ReturnType
            .Should()
            .Be(typeof(List<InventoryItem>));
        GetPublicBankMethod(nameof(Bank.GetDepositFailure), typeof(uint), typeof(uint))
            .ReturnType
            .Should()
            .Be(typeof(string));
        GetPublicBankMethod(nameof(Bank.DepositItem), typeof(uint), typeof(uint))
            .ReturnType
            .Should()
            .Be(typeof(InventoryItem));
        GetPublicBankMethod(nameof(Bank.WithdrawItem), typeof(uint), typeof(string))
            .ReturnType
            .Should()
            .Be(typeof(void));
        GetPublicBankMethod(nameof(Bank.GetCityBankAccessFailure), typeof(uint), typeof(uint))
            .ReturnType
            .Should()
            .Be(typeof(string));
        GetPublicBankMethod(nameof(Bank.SetCityBankId), typeof(uint), typeof(string))
            .ReturnType
            .Should()
            .Be(typeof(void));
        GetPublicBankMethod(nameof(Bank.NormalizeStorageId), typeof(InventoryItem))
            .ReturnType
            .Should()
            .Be(typeof(bool));
    }

    [Test]
    public void BankService_ConstrainsItemQueriesToGlobalBankStorage()
    {
        var query = (DBQuery<InventoryItem>)GetPrivateBankMethod("BuildPlayerItemQuery", typeof(string))
            .Invoke(null, new object[] { "player-id" })!;

        var queryString = query.BuildQuery().QueryString;

        queryString.Should().Contain("@StorageId:GLOBAL_BANK");
        queryString.Should().Contain("@PlayerId:player\\-id");
    }

    [Test]
    public void BankViewModel_DelegatesBankOperationsToBankService()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "BankViewModel.cs"));

        source.Should().Contain("Bank.GetItemCount(playerId)");
        source.Should().Contain("Bank.SearchItems(playerId, SearchText)");
        source.Should().Contain("Bank.GetDepositFailure(Player, item)");
        source.Should().Contain("Bank.DepositItem(Player, item)");
        source.Should().Contain("Bank.WithdrawItem(Player, itemId)");
        source.Should().Contain("Bank.GetItemCountText(itemCount)");
        source.Should().NotContain("InventoryItem.BankStorageId");
        source.Should().NotContain("BankStorage.");
        source.Should().NotContain("Bank.MaxItems");
        source.Should().NotContain("new DBQuery<InventoryItem>");
        source.Should().NotContain("ObjectPlugin.Serialize");
        source.Should().NotContain("ObjectPlugin.Deserialize");
        source.Should().NotContain("Item.IsLegacyItem");
        source.Should().NotContain("AddFieldSearch(nameof(InventoryItem.StorageId)");
    }

    [Test]
    public void RetiredBankStorageUpgradeType_IsRemovedFromPropertyUpgradeTypes()
    {
        var removedUpgradeName = "BankLevel";

        Enum.GetNames(typeof(PropertyUpgradeType)).Should().NotContain(removedUpgradeName);
        ((int)PropertyUpgradeType.MedicalCenterLevel).Should().Be(3);
        ((int)PropertyUpgradeType.StarportLevel).Should().Be(4);
        ((int)PropertyUpgradeType.CantinaLevel).Should().Be(5);
    }

    [Test]
    public void CityManagement_NoLongerExposesRetiredBankStorageUpgradeUiOrActions()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ManageCityDefinition.cs"));
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCityViewModel.cs"));
        var removedUpgradeAction = "UpgradeBank";
        var removedUpgradeEnabledFlag = "CanUpgradeBanks";
        var removedUpgradeName = "BankUpgrade";
        var removedUpgradeTooltip = "GetBankUpgrade";

        definition.Should().NotContain("Upgrade Banks");
        definition.Should().NotContain(removedUpgradeName);
        viewModel.Should().NotContain(removedUpgradeAction);
        viewModel.Should().NotContain(removedUpgradeEnabledFlag);
        viewModel.Should().NotContain(removedUpgradeTooltip);
    }

    [Test]
    public void BankBuildingTerminals_UseCityIdOnlyForAccessGate()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PropertyLayoutDefinition",
            "BankLayoutDefinition.cs"));

        source.Should().Contain("Service.Bank.GetCityBankAccessFailure(player, bank)");
        source.Should().Contain("Service.Bank.SetCityBankId(placeable, cityId)");
        source.Should().Contain("SetEventScript(placeable, EventScript.Placeable_OnUsed, ScriptName.OnOpenPropertyBank)");
        source.Should().NotContain("GetLocalString(bank");
        source.Should().NotContain("SetLocalString(placeable");
        source.Should().NotContain("StorageIdLocalName");
        source.Should().NotContain("StorageItemLimitLocalName");
        source.Should().NotContain("PropertyUpgradeType.BankLevel");
    }

    [Test]
    public void BankTerminalsInPaletteAndAreas_DoNotCarryStorageConfiguration()
    {
        var root = FindRepositoryRoot();
        var bankFiles = new List<string>
        {
            Path.Combine(root.FullName, "Module", "utp", "bank_term.utp.json")
        };

        bankFiles.AddRange(Directory
            .GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
            .Where(path => File.ReadAllText(path).Contains("\"value\": \"open_bank\"")));

        bankFiles.Should().NotBeEmpty();

        foreach (var file in bankFiles)
        {
            var jObject = JObject.Parse(File.ReadAllText(file));
            var bankVarTables = FindBankVarTables(jObject).ToList();

            bankVarTables.Should().NotBeEmpty(file);

            foreach (var table in bankVarTables)
            {
                table.Should().NotContainKey("STORAGE_ID", file);
                table.Should().NotContainKey("STORAGE_ITEM_LIMIT", file);
            }
        }
    }

    [Test]
    public void LoadHint_DescribesLinkedBankStorage()
    {
        const int customTlkOffset = 16777216;
        const int bankLoadHintTlkId = 79776;

        var root = FindRepositoryRoot();
        var loadHints = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "loadhints.2da"));
        var tlk = JObject.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_tlk",
            "sw_tlk.tlk.json")));
        var text = tlk["entries"]?
            .OfType<JObject>()
            .Single(entry => entry["id"]?.Value<int>() == bankLoadHintTlkId)["text"]?
            .Value<string>();

        loadHints.Should().Contain((customTlkOffset + bankLoadHintTlkId).ToString());
        text.Should().Contain("All bank terminals access the same storage");
        text.Should().NotContain("not linked");
    }

    [Test]
    public void LinkedBankStorageMigration_CleansRetiredUpgradeDataAndMigratesAllInventoryItems()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration");
        var helper = File.ReadAllText(Path.Combine(migrationRoot, "LinkedBankStorageMigration.cs"));
        var combatMigration = File.ReadAllText(Path.Combine(migrationRoot, "_22_CombatSystemReplacement.cs"));

        combatMigration.Should().Contain("LinkedBankStorageMigration.RemoveRetiredUpgradeData();");
        combatMigration.Should().Contain("LinkedBankStorageMigration.MigrateInventoryItemsToGlobalBank();");
        combatMigration.Should().Contain("StoredItemDataMigration.Migrate();");
        helper.Should().Contain("!validUpgradeKeys.Contains(property.Name)");
        helper.Should().Contain("!validNumericUpgradeKeys.Contains(property.Name)");
        helper.Should().Contain("Bank.NormalizeStorageId(item)");
        helper.Should().NotContain("Bank.StorageId");
        helper.Should().NotContain("BankStorage.Global");
        combatMigration.IndexOf("LinkedBankStorageMigration.RemoveRetiredUpgradeData();", StringComparison.Ordinal)
            .Should()
            .BeLessThan(combatMigration.IndexOf("StoredItemDataMigration.Migrate();", StringComparison.Ordinal));
        combatMigration.IndexOf("StoredItemDataMigration.Migrate();", StringComparison.Ordinal)
            .Should()
            .BeLessThan(combatMigration.IndexOf("LinkedBankStorageMigration.MigrateInventoryItemsToGlobalBank();", StringComparison.Ordinal));
    }

    private static IEnumerable<Dictionary<string, object>> FindBankVarTables(JToken token)
    {
        if (token is JObject jObject)
        {
            if (jObject["OnUsed"]?["value"]?.Value<string>() == "open_bank")
            {
                yield return jObject["VarTable"]?["value"] is JArray varTable
                    ? ReadVarTable(varTable)
                    : new Dictionary<string, object>();
            }

            foreach (var child in jObject.Properties().Select(property => property.Value))
            {
                foreach (var result in FindBankVarTables(child))
                {
                    yield return result;
                }
            }
        }
        else if (token is JArray jArray)
        {
            foreach (var child in jArray)
            {
                foreach (var result in FindBankVarTables(child))
                {
                    yield return result;
                }
            }
        }
    }

    private static Dictionary<string, object> ReadVarTable(JArray varTable)
    {
        var result = new Dictionary<string, object>();

        foreach (var entry in varTable.OfType<JObject>())
        {
            var name = entry["Name"]?["value"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var value = entry["Value"]?["value"];
            if (value == null)
                continue;

            result[name] = value.Type == JTokenType.Integer
                ? (object)value.Value<int>()
                : value.Value<string>();
        }

        return result;
    }

    private static MethodInfo GetPublicBankMethod(string name, params Type[] parameterTypes)
    {
        var method = typeof(Bank).GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        method.Should().NotBeNull();
        return method!;
    }

    private static MethodInfo GetPrivateBankMethod(string name, params Type[] parameterTypes)
    {
        var method = typeof(Bank).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        method.Should().NotBeNull();
        return method!;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        // Walk to the .sln rather than .git: in a git worktree .git is a file, not a directory.
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
