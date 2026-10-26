using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
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
    public void CharacterMigration15_InvokesLegacySaberMigration()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "CharacterMigration",
            "_14_15_CombatUpgradeMigrations.cs"));

        migration.Should().Contain("SavedLegacySaberMigration.MigrateCharacter(character);");
    }

    [Test]
    public void SubmissionToken_TransfersCraftedDamageButNotBlueprintScaffolding()
    {
        var root = FindRepositoryRoot();
        var serverRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");

        var workbench = File.ReadAllText(Path.Combine(serverRoot, "Service", "LightsaberWorkbench.cs"));
        workbench.Should().Contain("WeaponSubmissionTokenTag = \"wpn_sub_token\";");

        // The submission token blueprint must exist, stay a weapon submission token, and
        // carry no DMG of its own: every DMG point on a token comes from a crafted
        // enhancement, which is why the workbench may add it to the saber's tier DMG.
        var token = JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", "wpn_sub_token.uti.json")));
        token["Tag"]!["value"]!.Value<string>().Should().Be("wpn_sub_token");
        token["PropertiesList"]!["value"]!.Children<JObject>()
            .Select(p => p["PropertyName"]!["value"]!.Value<int>())
            .Should().NotContain((int)ItemPropertyType.DMG);

        // The workbench routes every token property through the shared filter and the
        // same merge the enhancement slots use.
        var viewModel = File.ReadAllText(Path.Combine(serverRoot, "Feature", "GuiDefinition", "ViewModel", "LightsaberWorkbenchViewModel.cs"));
        viewModel.Should().Contain("LightsaberWorkbench.WeaponSubmissionTokenTag");
        viewModel.Should().MatchRegex(@"ShouldTransferSubmissionProperty\([^;]*\)\)\s*continue;\s*Craft\.ApplyCraftedItemProperty\(item, ip\);");

        var cases = new (ItemPropertyType Type, int SubType, bool Transfers)[]
        {
            (ItemPropertyType.DMG, -1, true),
            (ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Electrical, true),
            (ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical, false),
            (ItemPropertyType.Accuracy, -1, true),
            (ItemPropertyType.Stamina, -1, true),
            (ItemPropertyType.Delay, -1, false),
            (ItemPropertyType.RequiresSkill, 0, false),
            (ItemPropertyType.UseLimitationPerk, 0, false),
        };
        foreach (var (type, subType, transfers) in cases)
        {
            LightsaberWorkbench.ShouldTransferSubmissionProperty(type, subType)
                .Should().Be(transfers, $"{type} (subtype {subType}) transfer must be {transfers}");
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
                     "ItemPropertyType.Accuracy",
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

        var weaponRoot = Path.Combine(root.FullName, "SWLOR_Haks", "sw_weapon");
        Directory.Exists(uiRoot).Should().BeTrue("the SWLOR_Haks sw_ui asset directory must be checked out");
        Directory.Exists(weaponRoot).Should().BeTrue("the SWLOR_Haks sw_weapon asset directory must be checked out");

        foreach (var weaponType in new[] { BaseItem.Lightsaber, BaseItem.Saberstaff })
        {
            foreach (var hilt in LightsaberWorkbench.GetHilts(weaponType))
            {
                hilt.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
                if (hilt.PreviewResref.StartsWith("ui_"))
                {
                    HakFileExists(uiRoot, $"{hilt.PreviewResref}.tga")
                        .Should().BeTrue($"preview texture {hilt.PreviewResref}.tga must exist in sw_ui");
                }
            }
        }

        var straightColors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false);
        var curvedColors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, true);
        var staffColors = LightsaberWorkbench.GetBladeColors(BaseItem.Saberstaff, false);

        straightColors.Should().HaveCount(43);
        staffColors.Should().HaveCount(40);
        curvedColors.Should().HaveCount(14, "legacy curved emitters keep their original model slots");

        straightColors.Select(c => c.StraightTopValue).Should().Contain(new[] { 191, 192, 193, 194 });
        straightColors.Select(c => c.StraightTopValue).Should().NotContain(new[] { 71, 72, 73, 74 });
        curvedColors.Select(c => c.CurvedTopValue).Should().Contain(new[] { 71, 72, 73, 74 });

        foreach (var color in straightColors.Concat(curvedColors).Concat(staffColors))
        {
            color.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
            (HakFileExists(uiRoot, $"{color.PreviewResref}.tga") ||
             HakFileExists(uiRoot, $"{color.PreviewResref}.dds"))
                .Should().BeTrue($"preview texture {color.PreviewResref} (.tga or .dds) must exist in sw_ui");
        }

        // Every selectable top value must have a blade model and inventory icon in sw_weapon.
        var topChecks = straightColors.Select(c => ("wswglsbr", c.StraightTopValue))
            .Concat(curvedColors.Select(c => ("wswglsbr", c.CurvedTopValue)))
            .Concat(staffColors.Select(c => ("wdblsbr", c.SaberstaffTopValue)));
        foreach (var (prefix, value) in topChecks)
        {
            HakFileExists(weaponRoot, $"{prefix}_t_{value:D3}.mdl")
                .Should().BeTrue($"blade model {prefix}_t_{value:D3}.mdl must exist in sw_weapon");
            (HakFileExists(weaponRoot, $"i{prefix}_t_{value:D3}.tga") ||
             HakFileExists(weaponRoot, $"i{prefix}_t_{value:D3}.dds"))
                .Should().BeTrue($"inventory icon i{prefix}_t_{value:D3}.tga must exist in sw_weapon");
        }

        // Saberstaff hilts use dedicated wiki renders shipped in sw_ui.
        foreach (var hilt in LightsaberWorkbench.GetHilts(BaseItem.Saberstaff))
        {
            hilt.PreviewResref.Length.Should().BeLessThanOrEqualTo(16);
            hilt.PreviewResref.Should().StartWith("ui_ssh_");
            HakFileExists(uiRoot, $"{hilt.PreviewResref}.tga")
                .Should().BeTrue($"preview texture {hilt.PreviewResref}.tga must exist in sw_ui");
        }
    }

    [Test]
    public void BladeColors_UseMatchingLightColorsOrNeutralWhiteFallback()
    {
        var expected = new Dictionary<string, LightColor>
        {
            ["Orange"] = LightColor.Orange,
            ["Blue"] = LightColor.Blue,
            ["Green 1"] = LightColor.Green,
            ["Red"] = LightColor.Red,
            ["White"] = LightColor.White,
            ["Yellow"] = LightColor.Yellow,
            ["Purple 1"] = LightColor.Purple,
            ["Teal"] = LightColor.White,
            ["Pink"] = LightColor.White,
            ["Brown"] = LightColor.White,
            ["Green 2"] = LightColor.Green,
            ["Purple 2"] = LightColor.Purple,
            ["Lavender"] = LightColor.White,
            ["Cyan"] = LightColor.White,
        };

        var colors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false)
            .ToDictionary(color => color.Name);
        colors.Keys.Should().Contain(expected.Keys);

        foreach (var (name, lightColor) in expected)
        {
            colors[name].LightColor.Should().Be(lightColor);
        }

        // Check every catalog entry, including shoto, crossguard and saberstaff pike variants.
        var allColors = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false)
            .Concat(LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, true))
            .Concat(LightsaberWorkbench.GetBladeColors(BaseItem.Saberstaff, false));
        foreach (var color in allColors)
        {
            var name = Regex.Replace(color.Name, @"^(shoto|crossguard|pike) - ", "");
            name = Regex.Replace(name, @" \(variant \d+\)$", "").ToLowerInvariant();
            var lightColor = name switch
            {
                "orange" => LightColor.Orange,
                "blue" => LightColor.Blue,
                "green" or "green 1" or "green 2" => LightColor.Green,
                "red" => LightColor.Red,
                "yellow" => LightColor.Yellow,
                "purple" or "purple 1" or "purple 2" => LightColor.Purple,
                "white" or "teal" or "pink" or "brown" or "lavender" or "cyan" => LightColor.White,
                _ => throw new InvalidOperationException($"Add an explicit light-color expectation for {color.Name}")
            };
            color.LightColor.Should().Be(lightColor, $"{color.Name} must use its supported light color or white fallback");
        }

        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "LightsaberWorkbenchViewModel.cs"));
        viewModel.Should().Contain("var lightColor = (LightColor)resolvedLightColor;");
        viewModel.Should().Contain("ApplyBladeLight(item, lightColor, lightBrightness);");
        Regex.Replace(viewModel, @"\s+", " ").Should().Contain("SelectedLightBrightnessIndex == 0 ? LightBrightness.Dim : LightBrightness.Low");
        viewModel.Should().Contain("WatchOnClient(model => model.SelectedLightColor);");
        viewModel.Should().Contain("WatchOnClient(model => model.SelectedLightBrightnessIndex);");
        viewModel.Should().Contain("SelectedLightBrightnessIndex = 0;", "the workbench defaults to Dim (5m) light");
        viewModel.Should().NotContain("SelectedLightBrightnessIndex = 1;");
        viewModel.Should().Contain("ItemPropertyLight(lightBrightness, lightColor)");
        viewModel.Should().Contain("GetItemPropertyType(ip) == ItemPropertyType.Light");
    }

    [Test]
    public void LightColor_MatchBladeFollowsBladeAndExplicitChoicesStick()
    {
        var blue = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false).Single(x => x.Name == "Blue");
        var red = LightsaberWorkbench.GetBladeColors(BaseItem.Lightsaber, false).Single(x => x.Name == "Red");

        // Default (Match Blade): the light follows whichever blade is selected.
        LightsaberWorkbench.ResolveLightColor(blue, LightsaberWorkbench.MatchBladeLightColor).Should().Be((int)LightColor.Blue);
        LightsaberWorkbench.ResolveLightColor(red, LightsaberWorkbench.MatchBladeLightColor).Should().Be((int)LightColor.Red);

        // An explicit color that equals the current blade's color still sticks after a blade change.
        var selection = (int)LightColor.Blue;
        LightsaberWorkbench.ResolveLightColor(blue, selection).Should().Be((int)LightColor.Blue);
        LightsaberWorkbench.ResolveLightColor(red, selection).Should().Be((int)LightColor.Blue);

        // Picking Match Blade again resumes following the blade.
        selection = LightsaberWorkbench.MatchBladeLightColor;
        LightsaberWorkbench.ResolveLightColor(red, selection).Should().Be((int)LightColor.Red);

        // Match Blade must not collide with a real light color.
        Enum.IsDefined(typeof(LightColor), LightsaberWorkbench.MatchBladeLightColor).Should().BeFalse();

        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "LightsaberWorkbenchViewModel.cs"));
        var normalized = Regex.Replace(viewModel, @"\s+", " ");

        // Match Blade is the default, resolved when constructing, and no inference from client echoes remains.
        normalized.Should().Contain("SelectedLightColor = LightsaberWorkbench.MatchBladeLightColor;");
        normalized.Should().Contain("LightsaberWorkbench.ResolveLightColor(SelectedTop, SelectedLightColor)");
        viewModel.Should().NotContain("_lightColorChosen");
        viewModel.Should().NotContain("_autoLightColor");
        viewModel.Should().NotContain("OnClientPropertyUpdated");
        viewModel.Should().NotContain("SelectedLightColor = (int)SelectedTop.LightColor;");

        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "LightsaberWorkbenchDefinition.cs"));
        var matchBladeOption = definition.IndexOf("AddOption(\"Match Blade\"", StringComparison.Ordinal);
        matchBladeOption.Should().BeGreaterThan(-1);
        matchBladeOption.Should().BeLessThan(definition.IndexOf("AddOption(\"Blue\"", StringComparison.Ordinal),
            "Match Blade must be the first Light Color entry");
    }

    [Test]
    public void Window_InitialHeightFitsA720pClient()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "LightsaberWorkbenchDefinition.cs"));

        definition.Should().Contain("SetInitialGeometry(0, 0, 545f, 660f)");
        definition.Should().Contain("SetIsResizable(true)");
    }

    [TestCase(BaseItem.Lightsaber, "", "ui_lsc_", new[] { 11, 12, 13, 14, 15, 21, 22, 23, 24, 51, 52, 53, 54, 55 })]
    [TestCase(BaseItem.Lightsaber, "shoto - ", "ui_lst_", new[] { 131, 132, 133, 134, 135, 141, 142, 143, 144, 161, 171, 172, 173, 174, 175 })]
    [TestCase(BaseItem.Lightsaber, "crossguard - ", "ui_lst_", new[] { 194, 192, 193, 75, 81, 82, 83, 84, 191, 111, 112, 113, 114, 115 })]
    [TestCase(BaseItem.Saberstaff, "", "ui_lsc_", new[] { 11, 12, 13, 14, 25, 21, 22, 23, 24, 51, 52, 53, 54, 143 })]
    [TestCase(BaseItem.Saberstaff, "shoto - ", "ui_sst_", new[] { 61, 62, 63, 64, 71, 72, 73, 74, 81, 82, 83, 84, 93 })]
    [TestCase(BaseItem.Saberstaff, "pike - ", "ui_sst_", new[] { 101, 102, 103, 104, 111, 112, 113, 114, 121, 122, 123, 124, 133 })]
    public void EmitterCategories_UseExpectedModelsAndPreviews(
        BaseItem weaponType, string category, string previewPrefix, int[] expectedModels)
    {
        var colors = LightsaberWorkbench.GetBladeColors(weaponType, false)
            .Where(color => category.Length == 0
                ? !color.Name.Contains(" - ")
                : color.Name.StartsWith(category, StringComparison.Ordinal))
            .ToList();

        colors.Select(color => LightsaberWorkbench.GetTopValue(color, weaponType, false))
            .Should().BeEquivalentTo(expectedModels, "each weapon category must keep its intended model slots");
        foreach (var color in colors)
        {
            color.PreviewResref.Should().StartWith(previewPrefix);
            if (category.Length != 0)
            {
                var model = LightsaberWorkbench.GetTopValue(color, weaponType, false);
                color.PreviewResref.Should().Be($"{previewPrefix}{model:D3}");
            }
        }

        if (weaponType == BaseItem.Saberstaff && category.Length == 0)
            colors.Single(color => color.SaberstaffTopValue == 143).Name.Should().Be("Cyan");
    }
    private static Func<string, bool> GetIsCraftableSaberResref()
    {
        var type = Type.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.LegacySaberMigration, SWLOR.Game.Server")!;
        var method = type.GetMethod("IsCraftableSaberResref", BindingFlags.Public | BindingFlags.Static)!;
        return resref => (bool)method.Invoke(null, new object[] { resref })!;
    }

    /// <summary>Resource names are case-insensitive, and some HAK files use mixed case.</summary>
    private static bool HakFileExists(string directory, string fileName) =>
        Directory.EnumerateFiles(directory, fileName, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).Any();

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
