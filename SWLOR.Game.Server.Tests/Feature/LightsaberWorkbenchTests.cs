using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class LightsaberWorkbenchTests
{
    private const int LightsaberBaseItem = 512;
    private const int SaberstaffBaseItem = 511;

    [Test]
    public void KyberToken_IsCursedPlotSingleStackAndActivatable()
    {
        var root = FindRepositoryRoot();
        var token = JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", "kyber_token.uti.json")));

        token["Tag"]!["value"]!.Value<string>().Should().Be("kyber_token");
        token["TemplateResRef"]!["value"]!.Value<string>().Should().Be("kyber_token");
        token["Cursed"]!["value"]!.Value<int>().Should().Be(1, "the token must be blocked from storage, market, and drops");
        token["Plot"]!["value"]!.Value<int>().Should().Be(1);
        token["StackSize"]!["value"]!.Value<int>().Should().Be(1);

        var properties = token["PropertiesList"]!["value"]!.Children<JObject>().ToList();
        properties.Should().ContainSingle("the token's only property is the self-activation that converts it into currency");
        properties[0]["PropertyName"]!["value"]!.Value<int>().Should().Be(15, "CastSpell makes the item usable");
        properties[0]["Subtype"]!["value"]!.Value<int>().Should().Be(335, "ACTIVATE_ITEM_SELF routes to the tag-based item definition");
    }

    [Test]
    public void KyberToken_ConvertsToCurrencyAndBenchConsumesCurrency()
    {
        var root = FindRepositoryRoot();
        var serverRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");

        var currencyType = File.ReadAllText(Path.Combine(serverRoot, "Service", "CurrencyService", "CurrencyType.cs"));
        currencyType.Should().Contain("KyberToken");

        var consumables = File.ReadAllText(Path.Combine(serverRoot, "Feature", "ItemDefinition", "ConsumableItemDefinition.cs"));
        consumables.Should().Contain("_builder.Create(\"kyber_token\")");
        consumables.Should().Contain("Currency.GiveCurrency(user, CurrencyType.KyberToken, 1);");

        var workbench = File.ReadAllText(Path.Combine(serverRoot, "Service", "LightsaberWorkbench.cs"));
        workbench.Should().Contain("Currency.GetCurrency(player, CurrencyType.KyberToken)");

        var viewModel = File.ReadAllText(Path.Combine(serverRoot, "Feature", "GuiDefinition", "ViewModel", "LightsaberWorkbenchViewModel.cs"));
        viewModel.Should().Contain("Currency.TakeCurrency(Player, CurrencyType.KyberToken, 1);");
    }

    [Test]
    public void WorkbenchSabers_MatchTierFiveTrainingSaberStats()
    {
        var root = FindRepositoryRoot();
        var utiRoot = Path.Combine(root.FullName, "Module", "uti");

        AssertSaberMatchesTemplate(
            Path.Combine(utiRoot, "ls_custom.uti.json"),
            Path.Combine(utiRoot, "saber_train_5.uti.json"),
            LightsaberBaseItem);
        AssertSaberMatchesTemplate(
            Path.Combine(utiRoot, "ss_custom.uti.json"),
            Path.Combine(utiRoot, "trn_saberstaff_5.uti.json"),
            SaberstaffBaseItem);
    }

    private static void AssertSaberMatchesTemplate(string saberPath, string templatePath, int expectedBaseItem)
    {
        var saber = JObject.Parse(File.ReadAllText(saberPath));
        var template = JObject.Parse(File.ReadAllText(templatePath));

        saber["BaseItem"]!["value"]!.Value<int>().Should().Be(expectedBaseItem);
        saber["Plot"]!["value"]!.Value<int>().Should().Be(1, "workbench sabers follow the training saber economy rules");

        var saberProperties = ExtractProperties(saber);
        var templateProperties = ExtractProperties(template);
        saberProperties.Should().BeEquivalentTo(templateProperties,
            $"{Path.GetFileName(saberPath)} must carry the exact tier 5 property set of {Path.GetFileName(templatePath)}");
    }

    private static List<(int PropertyName, int Subtype, int CostTable, int CostValue)> ExtractProperties(JObject item)
    {
        return item["PropertiesList"]!["value"]!
            .Children<JObject>()
            .Select(property => (
                property["PropertyName"]!["value"]!.Value<int>(),
                property["Subtype"]!["value"]!.Value<int>(),
                property["CostTable"]!["value"]!.Value<int>(),
                property["CostValue"]!["value"]!.Value<int>()))
            .ToList();
    }

    [Test]
    public void LegacySaberMigration_AllowlistCoversEveryCraftableSaberResref()
    {
        var root = FindRepositoryRoot();
        var isCraftable = GetIsCraftableSaberResref();

        // Every recipe-produced Lightsaber/Saberstaff base item blueprint must be
        // excluded from removal, otherwise the migration would reclaim craftable weapons.
        var recipeResrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "RecipeDefinition"),
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\.Resref\(""([^""]+)""\)"))
            {
                recipeResrefs.Add(match.Groups[1].Value);
            }
        }

        recipeResrefs.Should().NotBeEmpty();

        var craftableSabers = new List<string>();
        foreach (var resref in recipeResrefs)
        {
            var utiPath = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(utiPath))
                continue;

            var baseItem = JObject.Parse(File.ReadAllText(utiPath))["BaseItem"]!["value"]!.Value<int>();
            if (baseItem == LightsaberBaseItem || baseItem == SaberstaffBaseItem)
                craftableSabers.Add(resref);
        }

        craftableSabers.Should().NotBeEmpty("the training saber lines are recipe-produced sabers");
        var missing = craftableSabers.Where(resref => !isCraftable(resref)).ToList();
        missing.Should().BeEmpty("craftable sabers must never be reclaimed by the legacy saber migration");

        // The workbench outputs must be excluded as well.
        isCraftable("ls_custom").Should().BeTrue();
        isCraftable("ss_custom").Should().BeTrue();

        // DM-built sabers must not be excluded.
        isCraftable("lightsaber").Should().BeFalse();
        isCraftable("saberstaff").Should().BeFalse();
        isCraftable("malak_lightsaber").Should().BeFalse();
    }

    [Test]
    public void PlayerMigration15_InvokesLegacySaberMigration()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_15_RemoveObsoleteCombatInstructionDiscs.cs"));

        migration.Should().Contain("LegacySaberMigration.MigratePlayer(player);");
    }

    [Test]
    public void StoredItemDataMigration_ReplacesRootAndNestedSabersAcrossSurfaces()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));

        migration.Should().Contain("LegacySaberMigration.IsLegacySaber(obj)");
        migration.Should().Contain("LegacySaberMigration.MigrateStoredObject(obj, out var replacedSabers)");
        migration.Should().Contain("result.ReplacedRootSaber");
        migration.Should().Contain("item.IsListed = false;");
    }

    [Test]
    public void WorkbenchPlaceable_ExistsInAllThreeAreas()
    {
        var root = FindRepositoryRoot();

        var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "utp", "lightsaber_bench.utp.json")));
        blueprint["OnUsed"]!["value"]!.Value<string>().Should().Be("lsaber_bench");
        blueprint["Useable"]!["value"]!.Value<int>().Should().Be(1);
        blueprint["Plot"]!["value"]!.Value<int>().Should().Be(1);

        foreach (var area in new[] { "ar_scor_kacademy", "dan_jedienclave", "dath_hidtunnels" })
        {
            var git = JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "git", $"{area}.git.json")));
            var benches = git["Placeable List"]!["value"]!
                .Children<JObject>()
                .Where(placeable => placeable["Tag"]?["value"]?.Value<string>() == "lightsaber_bench")
                .ToList();

            benches.Should().ContainSingle($"{area} must contain exactly one lightsaber workbench");
            benches[0]["OnUsed"]!["value"]!.Value<string>().Should().Be("lsaber_bench");
        }
    }

    [Test]
    public void PartCatalog_PreviewTexturesExistAndTopValuesMatchHakModels()
    {
        var root = FindRepositoryRoot();
        var uiRoot = Path.Combine(root.FullName, "SWLOR_Haks", "sw_ui");

        if (!Directory.Exists(uiRoot))
        {
            Assert.Ignore("SWLOR_Haks submodule is not checked out.");
        }

        foreach (var weaponType in new[] { BaseItem.Lightsaber, BaseItem.Saberstaff })
        {
            foreach (var hilt in LightsaberWorkbench.GetHilts(weaponType))
            {
                hilt.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
                if (hilt.PreviewResref.StartsWith("ui_"))
                {
                    File.Exists(Path.Combine(uiRoot, $"{hilt.PreviewResref}.tga"))
                        .Should().BeTrue($"preview texture {hilt.PreviewResref}.tga must exist in sw_ui");
                }
            }
        }

        var straightColors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false);
        var curvedColors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, true);
        var staffColors = LightsaberWorkbench.GetBladeColors(BaseItem.Saberstaff, false);

        straightColors.Should().HaveCount(14);
        staffColors.Should().HaveCount(13, "White has no saberstaff blade model");
        curvedColors.Should().HaveCount(10, "the 02x color group has no curved blade models");

        foreach (var color in straightColors)
        {
            color.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
            File.Exists(Path.Combine(uiRoot, $"{color.PreviewResref}.tga"))
                .Should().BeTrue($"preview texture {color.PreviewResref}.tga must exist in sw_ui");
        }
    }

    private static Func<string, bool> GetIsCraftableSaberResref()
    {
        var type = Type.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.LegacySaberMigration, SWLOR.Game.Server")!;
        var method = type.GetMethod("IsCraftableSaberResref", BindingFlags.Public | BindingFlags.Static)!;
        return resref => (bool)method.Invoke(null, new object[] { resref })!;
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
