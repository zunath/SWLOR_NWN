using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

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
    public void WorkbenchSabers_MatchTierOneTrainingSaberStatsAndCarryTierVariable()
    {
        var root = FindRepositoryRoot();
        var utiRoot = Path.Combine(root.FullName, "Module", "uti");

        AssertSaberMatchesTemplate(
            Path.Combine(utiRoot, "ls_custom.uti.json"),
            Path.Combine(utiRoot, "saber_train_1.uti.json"),
            LightsaberBaseItem);
        AssertSaberMatchesTemplate(
            Path.Combine(utiRoot, "ss_custom.uti.json"),
            Path.Combine(utiRoot, "trn_saberstaff_1.uti.json"),
            SaberstaffBaseItem);

        // Bench sabers start at tier 1 so the tiered upgrade kits recognize them.
        foreach (var resref in new[] { "ls_custom", "ss_custom" })
        {
            var saber = JObject.Parse(File.ReadAllText(Path.Combine(utiRoot, $"{resref}.uti.json")));
            var tierVars = saber["VarTable"]!["value"]!
                .Children<JObject>()
                .Where(entry => entry["Name"]?["value"]?.Value<string>() == "SABER_TIER")
                .ToList();
            tierVars.Should().ContainSingle($"{resref} must carry the SABER_TIER variable");
            tierVars[0]["Value"]!["value"]!.Value<int>().Should().Be(1);
        }
    }

    private static void AssertSaberMatchesTemplate(string saberPath, string templatePath, int expectedBaseItem)
    {
        var saber = JObject.Parse(File.ReadAllText(saberPath));
        var template = JObject.Parse(File.ReadAllText(templatePath));

        saber["BaseItem"]!["value"]!.Value<int>().Should().Be(expectedBaseItem);
        saber["Plot"]!["value"]!.Value<int>().Should().Be(1, "workbench sabers follow the training saber economy rules");

        var saberProperties = ExtractProperties(saber);
        var templateProperties = ExtractProperties(template);

        // Workbench sabers receive a free 5m glow: Light (44), White (6),
        // iprp_lightcost (18), Dim (1). White is the safe blueprint fallback;
        // construction replaces it with the selected blade's supported color.
        var expectedProperties = templateProperties
            .Append((PropertyName: 44, Subtype: 6, CostTable: 18, CostValue: 1))
            .ToList();
        saberProperties.Should().BeEquivalentTo(expectedProperties,
            $"{Path.GetFileName(saberPath)} must carry the exact property set of {Path.GetFileName(templatePath)} plus a free Dim (5m) White Light property");
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
    public void SubmissionToken_TransfersStatsExcludingDamageProfile()
    {
        var root = FindRepositoryRoot();
        var serverRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");

        var workbench = File.ReadAllText(Path.Combine(serverRoot, "Service", "LightsaberWorkbench.cs"));
        workbench.Should().Contain("WeaponSubmissionTokenTag = \"wpn_sub_token\";");

        // The submission token blueprint must exist and stay a weapon submission token.
        var token = JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", "wpn_sub_token.uti.json")));
        token["Tag"]!["value"]!.Value<string>().Should().Be("wpn_sub_token");

        // The workbench transfers the token's stats but never its damage profile,
        // its own skill requirement, or its anti-equip Use Limitation: Perk lock.
        var viewModel = File.ReadAllText(Path.Combine(serverRoot, "Feature", "GuiDefinition", "ViewModel", "LightsaberWorkbenchViewModel.cs"));
        viewModel.Should().Contain("LightsaberWorkbench.WeaponSubmissionTokenTag");
        foreach (var excluded in new[]
                 {
                     "ItemPropertyType.DMG",
                     "ItemPropertyType.Delay",
                     "ItemPropertyType.WeaponDamageType",
                     "ItemPropertyType.RequiresSkill",
                     "ItemPropertyType.UseLimitationPerk",
                 })
        {
            viewModel.Should().Contain(excluded, $"the submission token transfer must skip {excluded}");
        }
    }

    [Test]
    public void SaberMigration_NormalizesLegacySabersAcrossSurfaces()
    {
        var root = FindRepositoryRoot();
        var migrationRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "MigrationDefinition");

        var storedItemMigration = File.ReadAllText(Path.Combine(migrationRoot, "ServerMigration", "StoredItemDataMigration.cs"));
        storedItemMigration.Should().Contain("LegacySaberMigration.MigrateStoredObject(obj, out var normalizedSabers)");

        // Legacy sabers are normalized in place - never removed or swapped.
        var saberMigration = File.ReadAllText(Path.Combine(migrationRoot, "LegacySaberMigration.cs"));
        saberMigration.Should().Contain("NormalizeSaber");
        saberMigration.Should().Contain("SABER_TIER");
        saberMigration.Should().NotContain("kyber_token", "the migration no longer grants tokens; legacy sabers are normalized in place");
        foreach (var normalizedProperty in new[]
                 {
                     "ItemPropertyType.DMG",
                     "ItemPropertyType.Delay",
                     "ItemPropertyType.RequiresSkill",
                     "ItemPropertyType.WeaponDamageType",
                     "ItemPropertyType.EnhancementBonus",
                     "ItemPropertyType.DamageBonus",
                     "ItemPropertyType.AccuracyBonus",
                 })
        {
            saberMigration.Should().Contain(normalizedProperty, $"{normalizedProperty} is part of the normalized damage profile");
        }

        // The recipes that taught the retired single-step kits have no equivalent to
        // convert into, so they're just cleaned up by the obsolete-item sweep.
        var obsoleteItems = File.ReadAllText(Path.Combine(migrationRoot, "ObsoleteItemMigration.cs"));
        foreach (var retired in new[] { "recipe_saberupg1", "recipe_staffupg1" })
        {
            obsoleteItems.Should().Contain($"\"{retired}\",");
        }

        // The retired single-step kits themselves are converted to the lowest tier of
        // the current tiered Engineering upgrade kit line rather than destroyed.
        foreach (var (legacyKit, lowestTierKit) in new[]
                 {
                     ("saber_upg1", "saber_upg2"),
                     ("saberstaff_upg1", "staff_upg2"),
                 })
        {
            obsoleteItems.Should().Contain($"\"{legacyKit}\", \"{lowestTierKit}\"",
                $"{legacyKit} must convert to {lowestTierKit} instead of being destroyed");
        }
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

            // Exact placement/count is a builder decision made in the toolset; the code
            // invariant is that each location has at least one correctly-wired workbench.
            benches.Should().NotBeEmpty($"{area} must contain a lightsaber workbench");
            benches.Should().OnlyContain(
                b => b["OnUsed"]!["value"]!.Value<string>() == "lsaber_bench",
                $"every lightsaber workbench in {area} must open the bench window");
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
        staffColors.Should().HaveCount(14, "every color has a saberstaff blade model");
        curvedColors.Should().HaveCount(14, "every color has a curved blade model");

        foreach (var color in straightColors.Concat(curvedColors).Concat(staffColors))
        {
            color.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
            File.Exists(Path.Combine(uiRoot, $"{color.PreviewResref}.tga"))
                .Should().BeTrue($"preview texture {color.PreviewResref}.tga must exist in sw_ui");
        }

        // Every selectable top value must have a blade model and inventory icon in sw_weapon.
        var weaponRoot = Path.Combine(root.FullName, "SWLOR_Haks", "sw_weapon");
        var topChecks = straightColors.Select(c => ("wswglsbr", c.StraightTopValue))
            .Concat(curvedColors.Select(c => ("wswglsbr", c.CurvedTopValue)))
            .Concat(staffColors.Select(c => ("wdblsbr", c.SaberstaffTopValue)));
        foreach (var (prefix, value) in topChecks)
        {
            File.Exists(Path.Combine(weaponRoot, $"{prefix}_t_{value:D3}.mdl"))
                .Should().BeTrue($"blade model {prefix}_t_{value:D3}.mdl must exist in sw_weapon");
            File.Exists(Path.Combine(weaponRoot, $"i{prefix}_t_{value:D3}.tga"))
                .Should().BeTrue($"inventory icon i{prefix}_t_{value:D3}.tga must exist in sw_weapon");
        }

        // Saberstaff hilts use dedicated wiki renders shipped in sw_ui.
        foreach (var hilt in LightsaberWorkbench.GetHilts(BaseItem.Saberstaff))
        {
            hilt.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
            hilt.PreviewResref.Should().StartWith("ui_ssh_");
            File.Exists(Path.Combine(uiRoot, $"{hilt.PreviewResref}.tga"))
                .Should().BeTrue($"preview texture {hilt.PreviewResref}.tga must exist in sw_ui");
        }
    }

    [Test]
    public void BladeColors_UseMatchingLightColorsOrNeutralWhiteFallback()
    {
        var expected = new Dictionary<string, LightColor>
        {
            ["Orange"] = LightColor.ORANGE,
            ["Blue"] = LightColor.BLUE,
            ["Green 1"] = LightColor.GREEN,
            ["Red"] = LightColor.RED,
            ["White"] = LightColor.WHITE,
            ["Yellow"] = LightColor.YELLOW,
            ["Purple 1"] = LightColor.PURPLE,
            ["Teal"] = LightColor.WHITE,
            ["Pink"] = LightColor.WHITE,
            ["Brown"] = LightColor.WHITE,
            ["Green 2"] = LightColor.GREEN,
            ["Purple 2"] = LightColor.PURPLE,
            ["Lavender"] = LightColor.WHITE,
            ["Cyan"] = LightColor.WHITE,
        };

        var colors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false)
            .ToDictionary(color => color.Name);
        colors.Keys.Should().BeEquivalentTo(expected.Keys);

        foreach (var (name, lightColor) in expected)
        {
            colors[name].LightColor.Should().Be(lightColor);
        }

        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "LightsaberWorkbenchViewModel.cs"));
        viewModel.Should().Contain("ApplyBladeLight(item, top.LightColor);");
        viewModel.Should().Contain("ItemPropertyLight(LightBrightness.LIGHTBRIGHTNESS_DIM, lightColor)");
        viewModel.Should().Contain("GetItemPropertyType(ip) == ItemPropertyType.Light");
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
