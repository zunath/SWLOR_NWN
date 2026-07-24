using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class SlicingContentTests
{
    private static readonly Dictionary<string, int> TerminalAreas = new()
    {
        ["czs220_maintlvl"] = 1, ["nanostation015"] = 1, ["viscarawildlands"] = 1,
        ["viscara_wwnorth"] = 2, ["viscaradeepmount"] = 2, ["v_cox_base"] = 2,
        ["korr_ravine"] = 3, ["korr_cavern"] = 3, ["korr_crypt_zil"] = 3,
        ["hutlar_qion"] = 4, ["pw_ar_narslum"] = 4, ["tat_anc_hillydes"] = 4,
        ["dan_jantacaves"] = 5, ["dath_mountains"] = 5, ["tat_wormden"] = 5,
    };

    [Test]
    public void DirectRewardCatalog_BlueprintsExistAndUseArmorRequirementsWithoutRawAttributes()
    {
        var root = FindRepositoryRoot();
        foreach (var reward in SlicingRewardCatalog.Entries)
        {
            var path = Path.Combine(root, "Module", "uti", reward.Resref + ".uti.json");
            File.Exists(path).Should().BeTrue($"{reward.Resref} is a direct slicing reward");

            if (reward.Category != SlicingRewardCategory.NamedItem)
                continue;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var properties = document.RootElement.GetProperty("PropertiesList").GetProperty("value");
            properties.EnumerateArray().Should().NotContain(property => GetInt(property, "PropertyName") == 0,
                $"{reward.Name} must not grant raw Might, Social, Vitality, Willpower, Perception, or Agility");

            var armorRequirement = properties.EnumerateArray().Single(property =>
                GetInt(property, "PropertyName") == 131 && GetInt(property, "Subtype") == 6);
            var expectedLevel = (reward.Tier - 1) * 10 + (reward.IsExceptional ? 5 : 0);
            GetInt(armorRequirement, "CostValue").Should().Be(expectedLevel,
                $"{reward.Name} scales from Armor rather than Espionage");
        }
    }

    [Test]
    public void TerminalAreas_HaveOneSharedTieredSpawnNode()
    {
        var root = FindRepositoryRoot();
        foreach (var (areaResref, tier) in TerminalAreas)
        {
            var path = Path.Combine(root, "Module", "git", areaResref + ".git.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var locals = document.RootElement.GetProperty("VarTable").GetProperty("value")
                .EnumerateArray()
                .ToDictionary(entry => GetString(entry, "Name"), entry => entry.GetProperty("Value").GetProperty("value"));

            locals["SLICING_TERMINAL_SPAWN_TABLE_ID"].GetString().Should().Be($"SLICING_TERMINAL_T{tier}");
            locals["SLICING_TERMINAL_SPAWN_COUNT"].GetInt32().Should().Be(1);
        }
    }

    [Test]
    public void TerminalSpawnTables_UseTierBlueprintsAndFortyFiveToSeventyFiveMinuteRespawns()
    {
        var tables = new SlicingTerminalSpawnDefinition().BuildSpawnTables();
        tables.Should().HaveCount(5);
        for (var tier = 1; tier <= 5; tier++)
        {
            var table = tables[$"SLICING_TERMINAL_T{tier}"];
            table.RespawnDelayMinutes.Should().Be(45);
            table.RespawnDelayMaximumMinutes.Should().Be(75);
            table.Spawns.Should().ContainSingle(spawn => spawn.Resref == $"slice_term_{tier}");
        }
    }

    [Test]
    public void Lockboxes_AreUsableMiscellaneousItemsRatherThanContainers()
    {
        var root = FindRepositoryRoot();
        var expectedIcons = new Dictionary<int, int>
        {
            [1] = 153,
            [2] = 156,
            [3] = 152,
            [4] = 154,
            [5] = 155,
        };
        for (var tier = 1; tier <= 5; tier++)
        {
            var path = Path.Combine(root, "Module", "uti", $"lockbox_t{tier}.uti.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var blueprint = document.RootElement;

            GetInt(blueprint, "BaseItem").Should().Be((int)BaseItem.MiscSmall);
            GetInt(blueprint, "ModelPart1").Should().Be(expectedIcons[tier]);
            GetInt(blueprint, "xModelPart1").Should().Be(expectedIcons[tier]);
            File.Exists(Path.Combine(root, "SWLOR_Haks", "sw_item", $"iit_smlmisc_{expectedIcons[tier]:000}.tga"))
                .Should().BeTrue($"tier {tier} uses a compatible icon from the pinned HAK");

            var properties = blueprint.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().ToList();
            properties.Should().ContainSingle();
            GetInt(properties[0], "PropertyName").Should().Be(15, "the item needs the Cast Spell property");
            GetInt(properties[0], "CostTable").Should().Be(3);
            GetInt(properties[0], "CostValue").Should().Be(13);
            GetInt(properties[0], "Subtype").Should().Be(335, "this is Activate Item (self) in iprp_spells.2da");

            var description = blueprint.GetProperty("Description").GetProperty("value").GetProperty("0").GetString();
            description.Should().Contain("Right-click this item in your inventory");
            description.Should().Contain("Activate Item");
        }
    }

    [Test]
    public void SlicingRecipes_CoverAllCraftsAndPreserveAgricultureAsAPoisonComponentSource()
    {
        AssertRecipeSet(new SlicingCacheSmitheryRecipes().BuildRecipes(), SkillType.Smithery, 5, RecipeEnhancementType.Armor);
        AssertRecipeSet(new SlicingCacheCookingRecipes().BuildRecipes(), SkillType.Agriculture, 5, RecipeEnhancementType.Food);
        AssertRecipeSet(new TraceFuseRecipes().BuildRecipes(), SkillType.Engineering, 5, RecipeEnhancementType.None);
        AssertRecipeSet(new SlicingTerminalFurnitureRecipes().BuildRecipes(), SkillType.Fabrication, 5, RecipeEnhancementType.Structure);

        var concentrates = new ConcentratedVenomRecipes().BuildRecipes().Values.ToList();
        concentrates.Should().HaveCount(5);
        concentrates.Should().OnlyContain(recipe =>
            recipe.Skill == SkillType.Espionage &&
            recipe.Quantity == 1 &&
            recipe.Components.Keys.Any(component => component.StartsWith("herb_")));
    }

    [Test]
    public void SlicingProvisions_UseTheRegisteredFoodItemTag()
    {
        var root = FindRepositoryRoot();
        foreach (var resref in new[]
                 {
                     "food_quietwatch", "food_dustveil", "food_tombwalk", "food_snowblind", "food_nightmarch"
                 })
        {
            var path = Path.Combine(root, "Module", "uti", resref + ".uti.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            GetString(document.RootElement, "Tag").Should().Be("FOOD",
                $"{resref} must route through ConsumableItemDefinition when used");
            GetString(document.RootElement, "TemplateResRef").Should().Be(resref);
        }
    }

    [Test]
    public void SlicingStructures_HaveOneStorageAndUnusedAppearances()
    {
        var root = FindRepositoryRoot();
        var expected = new Dictionary<StructureType, (string Resref, int Appearance)>
        {
            [StructureType.RustlineDataTerminal] = ("slc_rustterm", 6031),
            [StructureType.CipherfileCabinet] = ("slc_ciphcab", 30935),
            [StructureType.ListeningPostMonitor] = ("slc_listmon", 7201),
            [StructureType.GhostChannelConsole] = ("slc_ghostcon", 21450),
            [StructureType.BlacksiteAnalysisStation] = ("slc_blackstat", 30611),
        };

        var appearances = Directory.EnumerateFiles(Path.Combine(root, "Module", "utp"), "*.utp.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .ToList();
        try
        {
            foreach (var (type, detail) in expected)
            {
                var attribute = typeof(StructureType).GetField(type.ToString())!.GetCustomAttribute<StructureAttribute>();
                attribute.Should().NotBeNull();
                attribute!.Resref.Should().Be(detail.Resref);
                attribute.ItemStorage.Should().Be(1);
                appearances.Count(document => GetInt(document.RootElement, "Appearance") == detail.Appearance).Should().Be(1,
                    $"appearance {detail.Appearance} was selected because it had no existing module use");
            }
        }
        finally
        {
            foreach (var document in appearances)
                document.Dispose();
        }
    }

    [Test]
    public void SlicingNuiArt_HasEveryThemeTypeOrientationAndState()
    {
        var ui = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_ui");
        foreach (var theme in new[] { 'l', 't' })
        {
            AssertTga(Path.Combine(ui, $"slc_bg_{theme}.tga"), 640, 96);
            AssertTga(Path.Combine(ui, $"slc_goal_{theme}.tga"), 640, 96);
            foreach (var type in new[] { 's', 'c', 'j', 'x', 'e', 'o', 'b', 'q' })
            foreach (var orientation in Enumerable.Range(0, 4))
            foreach (var state in new[] { 'u', 'p', 's', 'd' })
            {
                var resref = $"slc{theme}{type}{orientation}{state}";
                resref.Length.Should().BeLessThanOrEqualTo(16);
                AssertTga(Path.Combine(ui, resref + ".tga"), 72, 72);
            }

            foreach (var type in new[] { 'e', 'o' })
            foreach (var orientation in Enumerable.Range(0, 4))
            foreach (var state in new[] { 'u', 'p', 's', 'd' })
            {
                var resref = $"slcg{theme}{type}{orientation}{state}";
                resref.Length.Should().BeLessThanOrEqualTo(16);
                AssertTga(Path.Combine(ui, resref + ".tga"), 72, 72);
            }
        }
    }

    [Test]
    public void SlicingNuiLayout_IsResizableScrollableAndUsesAStaticScalarBoundGrid()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "SlicingDefinition.cs"));
        var viewModel = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "SlicingViewModel.cs"));

        definition.Should().Contain("private const float TileSize = 56f;");
        definition.Should().Contain(".SetIsResizable(true)");
        definition.Should().Contain("wrapper.SetShowBorder(false)");
        definition.Should().Contain(".SetScrollbars(NuiScrollbars.Auto)");
        definition.Should().Contain("Keep the board as five ordinary rows of five buttons.");
        definition.Should().Contain("the transposed row-of-columns layout collapse");
        definition.Should().NotContain(".AddList(", "list templates collapse this particular live NUI window");
        definition.Should().Contain("for (var tileRow = 0; tileRow < 5; tileRow++)");
        definition.Should().Contain("column.AddRow(row => AddTileRow(row, rowIndex));");
        definition.Should().Contain("private static void AddTileRow");
        definition.Should().NotContain("row.AddColumn(", "nested board columns collapse in the live NUI client");
        definition.Should().NotContain(".BindIsVisible(", "all 25 fixed cells must participate in initial layout");
        definition.Should().Contain(".BindOnClicked(model => model.OnTile(tileRow, columnIndex))");
        definition.Should().Contain(".SetHeight(TileSize)");
        definition.Should().Contain(".SetWidth(TileSize)");

        viewModel.Should().NotContain("RestoreFixedWindowGeometry");
        viewModel.Should().Contain("EnsureUsableWindowGeometry");
        viewModel.Should().Contain("var slot = row * 5 + column;");
        viewModel.Should().Contain("Set(image, $\"TileImage{slot}\");");
        viewModel.Should().NotContain("GuiBindingList<string> TileColumn");
        viewModel.Should().NotContain("NuiGetEventArrayIndex()");

        for (var slot = 0; slot < 25; slot++)
        {
            typeof(SlicingViewModel).GetProperty($"TileImage{slot}").Should().NotBeNull();
            typeof(SlicingViewModel).GetProperty($"TileTooltip{slot}").Should().NotBeNull();
        }
    }

    [Test]
    public void SlicingHelp_ExplainsTheObjectiveControlsAndTraceCosts()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "SlicingDefinition.cs"));
        var viewModel = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "SlicingViewModel.cs"));

        definition.Should().Contain(".SetText(\"?\")");
        definition.Should().Contain(".BindOnClicked(model => model.OnHelp())");
        definition.Should().Contain("amber START / Entry tile to the magenta GOAL / Core tile");
        definition.Should().Contain("bright diamond outline");
        definition.Should().Contain("Click the selected tile again to rotate it clockwise. This costs 1 Trace.");
        definition.Should().Contain("directly above, below, left, or right");
        definition.Should().Contain("This costs 2 Trace.");
        definition.Should().Contain("There is no double-click action.");
        definition.Should().Contain("A rotation or swap commits the attempt.");
        definition.Should().Contain("Before commitment, closing or aborting is safe.");
        definition.Should().Contain("After commitment, running out of Trace, closing, or aborting counts as a failure");
        definition.Should().Contain(".BindOnClicked(model => model.OnCloseHelp())");

        viewModel.Should().Contain("public const string HelpPartial = \"SLICING_HELP\";");
        viewModel.Should().Contain("ChangeView(HelpPartial)");
        viewModel.Should().Contain("ChangeView(\"%%WINDOW_MAIN%%\")");
        viewModel.Should().Contain("UpdatePropertyFromClient(nameof(Geometry));");
        viewModel.Should().Contain("ChangePartialView(\"_window_\", partialName);");
    }

    [Test]
    public void SlicingSelection_RemainsVisibleWhenIntegrityUsesDamagedArt()
    {
        var board = Slicing.BuildBoard(1, 982451653);
        var session = new SlicingSession.ActiveSlicingSession
        {
            Source = SlicingSourceType.Lockbox,
            Board = board,
            SelectedIndex = 0
        };

        InvokeViewModelMethod<string>("GetTileImage", null, session, 0, true, 50)
            .Should().EndWith("s", "selected art must take priority so the documented white brackets remain visible");
        InvokeViewModelMethod<string>("GetTileImage", null, session, 1, true, 50)
            .Should().EndWith("d", "unselected tiles should still communicate the target's damaged state");
    }

    [Test]
    public void SlicingEndpoints_UseExplicitStartAndGoalArt()
    {
        var board = Slicing.BuildBoard(1, 982451653);
        var entry = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Entry);
        var core = board.Tiles.FindIndex(tile => tile.Type == SlicingTileType.Core);
        var route = board.Tiles.FindIndex(tile =>
            tile.Type is not SlicingTileType.Entry and not SlicingTileType.Core);
        var session = new SlicingSession.ActiveSlicingSession
        {
            Source = SlicingSourceType.Lockbox,
            Board = board
        };

        InvokeViewModelMethod<string>("GetTileImage", null, session, entry, true, 100)
            .Should().StartWith("slcgle", "Entry uses the visually explicit START asset family");
        InvokeViewModelMethod<string>("GetTileImage", null, session, core, false, 100)
            .Should().StartWith("slcglo", "Core uses the visually explicit GOAL asset family");
        InvokeViewModelMethod<string>("GetTileImage", null, session, route, false, 100)
            .Should().StartWith("slcl", "ordinary circuit tiles retain the original visual family");
        InvokeViewModelMethod<string>("GetTileTooltip", null, session, entry)
            .Should().StartWith("START / Entry");
        InvokeViewModelMethod<string>("GetTileTooltip", null, session, core)
            .Should().StartWith("GOAL / Core");
    }

    [Test]
    public void SlicingWindowGeometry_RecoversLegacyCollapsedSizeWithoutResettingNormalResizes()
    {
        var viewModel = new SlicingViewModel
        {
            Geometry = new GuiRectangle(17f, 23f, 80f, 31f)
        };

        InvokeViewModelMethod<object>("EnsureUsableWindowGeometry", viewModel);

        viewModel.Geometry.X.Should().Be(17f);
        viewModel.Geometry.Y.Should().Be(23f);
        viewModel.Geometry.Width.Should().Be(320f);
        viewModel.Geometry.Height.Should().Be(240f);

        viewModel.Geometry = new GuiRectangle(29f, 31f, 440f, 350f);
        InvokeViewModelMethod<object>("EnsureUsableWindowGeometry", viewModel);

        viewModel.Geometry.X.Should().Be(29f);
        viewModel.Geometry.Y.Should().Be(31f);
        viewModel.Geometry.Width.Should().Be(440f);
        viewModel.Geometry.Height.Should().Be(350f);
    }

    [Test]
    public void SlicingWindowLaunch_OnlyTethersPlacedTerminals()
    {
        var root = FindRepositoryRoot();
        var lockboxSource = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "ItemDefinition", "LockboxItemDefinition.cs"));
        var terminalSource = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "SlicingTerminal.cs"));

        lockboxSource.Should().Contain("Gui.TogglePlayerWindow(user, GuiWindowType.Slicing, payload);");
        lockboxSource.Should().NotContain("Gui.TogglePlayerWindow(user, GuiWindowType.Slicing, payload, item);");
        terminalSource.Should().Contain("Gui.TogglePlayerWindow(player, GuiWindowType.Slicing, payload, terminal);");
    }

    private static void AssertRecipeSet(
        Dictionary<RecipeType, RecipeDetail> recipes,
        SkillType skill,
        int count,
        RecipeEnhancementType enhancementType)
    {
        recipes.Should().HaveCount(count);
        recipes.Values.Should().OnlyContain(recipe => recipe.Skill == skill);
        if (enhancementType == RecipeEnhancementType.None)
            recipes.Values.Should().OnlyContain(recipe => recipe.EnhancementSlots == 0);
        else
            recipes.Values.Should().OnlyContain(recipe => recipe.EnhancementType == enhancementType && recipe.EnhancementSlots == 1);
    }

    private static void AssertTga(string path, int width, int height)
    {
        File.Exists(path).Should().BeTrue(path);
        var header = File.ReadAllBytes(path).Take(18).ToArray();
        header.Should().HaveCount(18);
        (header[12] | header[13] << 8).Should().Be(width);
        (header[14] | header[15] << 8).Should().Be(height);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static int GetInt(JsonElement element, string property) =>
        element.GetProperty(property).GetProperty("value").GetInt32();

    private static string GetString(JsonElement element, string property) =>
        element.GetProperty(property).GetProperty("value").GetString() ?? string.Empty;

    private static T InvokeViewModelMethod<T>(string name, SlicingViewModel instance, params object[] arguments)
    {
        var flags = BindingFlags.NonPublic |
                    (instance == null ? BindingFlags.Static : BindingFlags.Instance);
        var method = typeof(SlicingViewModel).GetMethod(name, flags)!;
        return (T)method.Invoke(instance, arguments)!;
    }
}
