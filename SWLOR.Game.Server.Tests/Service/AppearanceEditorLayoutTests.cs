using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Checks the constructed Appearance Editor controls: body map, colour column with the
/// selected part's picture, and the part browser window. The shared validator catches fatal
/// height constraints; these tests check bindings, event routing and authored size budgets.
/// They do not claim to run the client's layout solver.
/// </summary>
public class AppearanceEditorLayoutTests
{
    private const string WindowMain = "%%WINDOW_MAIN%%";

    private GuiConstructedWindow _window;
    private IReadOnlyDictionary<string, IGuiWidget> _partials;

    private static readonly string[] EditorPanels =
    {
        AppearanceEditorViewModel.EditorMainPartial, AppearanceEditorViewModel.EditorArmorPartial,
        AppearanceEditorViewModel.EditorWeaponPartial, AppearanceEditorViewModel.SettingsPartial,
        AppearanceEditorViewModel.OutfitsPartial
    };

    private static readonly string[] AllPanels = EditorPanels;

    private static readonly AppearanceEditorSlot[] ArmorOnlySlots =
    {
        AppearanceEditorSlot.Neck, AppearanceEditorSlot.Belt, AppearanceEditorSlot.Robe,
        AppearanceEditorSlot.Cloak, AppearanceEditorSlot.RightShoulder, AppearanceEditorSlot.LeftShoulder
    };

    private static readonly AppearanceArmorColor[] Channels =
    {
        AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1, AppearanceArmorColor.Metal1,
        AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2, AppearanceArmorColor.Metal2
    };

    [SetUp]
    public void BuildActualDefinitionWithoutEngineSerialization()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definition = new AppearanceEditorDefinition();
        _window = definition.BuildWindow();

        // Validation-only windows deliberately omit serialized partials. Read
        // the exact widget tree the builder validated instead of parsing source
        // text or recreating a second version of the appearance layout.
        var builder = typeof(AppearanceEditorDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        var authoredWindow = (GuiWindow<AppearanceEditorViewModel>)builder.GetType()
            .GetField("_activeWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
        _partials = authoredWindow.PartialViews;
    }

    // ------------------------------------------------------------------ whole window

    [Test]
    public void EveryPartialIsDefinedAndHasNoLayoutFindings()
    {
        _partials.Keys.Should().Contain(AllPanels.Append(WindowMain));
        _partials.Keys.Should().NotContain(new[]
        {
            "APPEARANCE_EDITOR_COLORS_CLOTH_LEATHER", "APPEARANCE_EDITOR_COLORS_METAL"
        }, "the armor palette is no longer swapped between nested palette partials");
        _window.LayoutFindings.Should().BeEmpty();
        foreach (var name in _partials.Keys)
        {
            GuiLayoutValidator.Validate("appearance_editor",
                    new Dictionary<string, IGuiWidget> { [name] = _partials[name] })
                .Should().BeEmpty($"{name} must not contain an unsolvable row");
        }
    }

    [Test]
    public void EveryEventWidgetHasAUniqueIdAndSharedIdsRouteToTheSameHandler()
    {
        var handlers = new Dictionary<string, string>();
        foreach (var name in AllPanels)
        {
            var eventWidgets = Walk(_partials[name]).Where(widget => widget.Events.Count > 0).ToArray();
            eventWidgets.Should().NotBeEmpty($"{name} has clickable controls");
            eventWidgets.Select(widget => widget.Id).Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id),
                $"NWN only fires events for elements with ids ({name})");
            eventWidgets.Select(widget => widget.Id).Should().OnlyHaveUniqueItems(
                $"an event id must identify one control in {name}");

            // Events are registered per window and element id, so a control repeated in
            // several partials (browser, palette, body map) must route identically.
            foreach (var (id, handler) in EventSnapshot(_partials[name]))
            {
                if (handlers.TryGetValue(id, out var existing))
                    handler.Should().Be(existing, $"{id} is registered once for the whole window");
                else
                    handlers[id] = handler;
            }
        }
    }

    [Test]
    public void RuntimePanelsRetainEveryBootEventAndBinding([ValueSource(nameof(AllPanels))] string partialName)
    {
        var boot = _partials[partialName];
        var runtime = AppearanceEditorDefinition.BuildEditorPanel(partialName);
        EventSnapshot(runtime).Should().BeEquivalentTo(EventSnapshot(boot),
            "runtime copies must route to the event handlers registered at boot, with the same typed arguments");
        BindingSnapshot(runtime).Should().BeEquivalentTo(BindingSnapshot(boot),
            "panel copies must retain every binding, including draw-list regions and outlines");
    }

    [Test]
    public void StaticPanelsRequireNoWindowGeometry()
    {
        var factory = typeof(AppearanceEditorDefinition).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(AppearanceEditorDefinition.BuildEditorPanel));
        factory.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(new[] { typeof(string) },
            "the panel layout must not be computed from the window size");
        foreach (var name in AllPanels)
        {
            var panel = AppearanceEditorDefinition.BuildEditorPanel(name);
            Walk(panel).Select(widget => (widget.GetType(), Width(widget), widget.DeclaredHeight))
                .Should().Equal(Walk(_partials[name]).Select(widget =>
                    (widget.GetType(), Width(widget), widget.DeclaredHeight)),
                    "reopening or switching tabs must reuse the boot layout's dimensions");
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => AppearanceEditorDefinition.BuildEditorPanel("unknown"));
    }

    [Test]
    public void NavigationAndMainContentHaveNoFixedWidthAncestor()
    {
        var main = _partials[WindowMain];
        var toggles = Walk(main).OfType<GuiToggles<AppearanceEditorViewModel>>().ToArray();
        var primary = toggles.Single(toggle => ReadProperty<string>(toggle, "SelectedValueBindName") ==
            nameof(AppearanceEditorViewModel.EditorTabToggleValue));
        var settings = toggles.Single(toggle => ReadProperty<string>(toggle, "SelectedValueBindName") ==
            nameof(AppearanceEditorViewModel.SettingsTabToggleValue));
        PathTo(main, primary).SkipLast(1).Should().OnlyContain(widget => Width(widget) == 0f);
        Width(primary).Should().Be(AppearanceEditorDefinition.TabWidth * 2);
        Width(settings).Should().Be(AppearanceEditorDefinition.TabWidth);
        AssertNavigation(PathTo(main, primary).OfType<GuiRow<AppearanceEditorViewModel>>().Last());
        var slot = Walk(main).Single(widget => widget.Id == AppearanceEditorViewModel.MainPartialElement);
        PathTo(main, slot).Should().OnlyContain(widget => Width(widget) == 0f,
            "the active partial needs the full available window width");
        Slots(main).Should().Equal(AppearanceEditorViewModel.MainPartialElement);
    }

    // ------------------------------------------------------------------ editor columns

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    [TestCase(AppearanceEditorViewModel.EditorWeaponPartial)]
    public void EditorsShowFixedColumnsAndThePartPicture(string partialName)
    {
        var partial = _partials[partialName];
        ReadProperty<NuiScrollbars>(partial, "Scrollbars").Should().Be(NuiScrollbars.None);
        Width(partial).Should().Be(0f);
        partial.DeclaredHeight.Should().Be(0f);
        var column = partial.Elements.Should().ContainSingle().Which;
        column.Should().BeOfType<GuiColumn<AppearanceEditorViewModel>>();

        var rows = column.Elements.ToList();
        Walk(partial).OfType<GuiComboBox<AppearanceEditorViewModel>>().Where(combo => combo.Id != "ae_quick_outfit")
            .Should().BeEmpty("the equipment slots are tabs, not a list");
        if (partialName == AppearanceEditorViewModel.EditorMainPartial)
        {
            // No hidden rows above the editors: a hidden row still takes up height.
            rows.Should().ContainSingle();
        }
        else
        {
            rows.Should().HaveCount(2, "the slot tabs sit above the editor");
            AssertEquipmentTabs(rows.First());
        }
        var content = rows.Last();
        ReadProperty<string>(content, "IsVisibleBindName").Should().Be(nameof(AppearanceEditorViewModel.HasItemEquipped));

        var groups = content.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>().ToArray();
        var expectedWidths = partialName == AppearanceEditorViewModel.EditorWeaponPartial
            ? new[] { AppearanceEditorDefinition.WeaponCategoryWidth, AppearanceEditorDefinition.PreviewColumnWidth + 32f }
            : new[] { AppearanceEditorDefinition.MapWidth, AppearanceEditorDefinition.ColoursWidth };
        groups.Select(group => Width(group)).Should().Equal(expectedWidths, "the editor's columns have fixed widths");
        Slots(partial).Should().BeEmpty("the part pictures are browsed in their own window");
        Walk(partial).Should().NotContain(widget => widget.Id != null && widget.Id.StartsWith("ae_tile_"));

        var preview = Walk(partial).Single(widget => widget.Id == "ae_part_preview");
        PathTo(partial, preview).Should().Contain(groups.Last(), "the part picture sits in the last column");
    }

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    [TestCase(AppearanceEditorViewModel.EditorWeaponPartial)]
    public void ThePartPictureOpensTheBrowserAndStepsAndTurnsParts(string partialName)
    {
        var partial = _partials[partialName];
        var preview = Walk(partial).Single(widget => widget.Id == "ae_part_preview");
        preview.Should().BeOfType<GuiButtonImage<AppearanceEditorViewModel>>();
        ReadProperty<string>(preview, "ResrefBindName").Should().Be(nameof(AppearanceEditorViewModel.PartPreviewImage));
        ReadProperty<string>(preview, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsPartPreviewEnabled));
        Width(preview).Should().Be(AppearanceEditorDefinition.TileSize);
        preview.DeclaredHeight.Should().Be(AppearanceEditorDefinition.TileSize);
        AssertSingleHandler(preview, "click", nameof(AppearanceEditorViewModel.OnClickPartPreview));

        Walk(partial).OfType<GuiLabel<AppearanceEditorViewModel>>()
            .Should().ContainSingle(label => ReadProperty<string>(label, "TextBindName") == nameof(AppearanceEditorViewModel.PartPreviewLabel));
        AssertSingleHandler(Walk(partial).Single(widget => widget.Id == "ae_part_previous"), "click",
            nameof(AppearanceEditorViewModel.OnClickPreviousPart));
        AssertSingleHandler(Walk(partial).Single(widget => widget.Id == "ae_part_next"), "click",
            nameof(AppearanceEditorViewModel.OnClickNextPart));
        var view = Walk(partial).Single(widget => widget.Id == "ae_part_view");
        ReadProperty<string>(view, "TextBindName").Should().Be(nameof(AppearanceEditorViewModel.PreviewViewText));
        ReadProperty<string>(view, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsPreviewViewChoiceVisible),
            "weapon parts have only front pictures");
        AssertSingleHandler(view, "click", nameof(AppearanceEditorViewModel.OnClickPreviewView));

        var column = PathTo(partial, preview).OfType<GuiColumn<AppearanceEditorViewModel>>().Last();
        Width(column).Should().Be(AppearanceEditorDefinition.PreviewColumnWidth);
        NaturalWidth(column).Should().BeLessThanOrEqualTo(AppearanceEditorDefinition.PreviewColumnWidth - 16f,
            "the picture and its buttons fit their column");
    }

    private static void AssertEquipmentTabs(IGuiWidget row)
    {
        ReadProperty<string>(row, "IsVisibleBindName").Should().BeNullOrEmpty("an empty slot still needs the tabs to pick another");
        var tabs = Walk(row).OfType<GuiToggles<AppearanceEditorViewModel>>().Should().ContainSingle().Which;
        ReadProperty<string>(tabs, "SelectedValueBindName").Should().Be(nameof(AppearanceEditorViewModel.EquipmentTypeValue));
        ReadProperty<List<string>>(tabs, "ElementLabels").Should().Equal(new[] { "Armor", "Main Hand", "Off Hand" },
            "a tab reports its position, which is the equipment value");
        Width(tabs).Should().Be(AppearanceEditorDefinition.TabWidth * 3);
        foreach (var (entry, itemType) in new[] { (0, 0), (1, 3), (2, 4) })
        {
            AppearanceEditorViewModel.ToItemTypeIndex(entry).Should().Be(itemType);
            AppearanceEditorViewModel.ToEquipmentTypeValue(itemType).Should().Be(entry);
        }
        AppearanceEditorViewModel.ToEquipmentTypeValue(1).Should().Be(0, "a helmet belongs to the armor tab");
        AppearanceEditorViewModel.ToEquipmentTypeValue(2).Should().Be(0, "a cloak belongs to the armor tab");
        var message = Walk(row).OfType<GuiLabel<AppearanceEditorViewModel>>().Should().ContainSingle().Which;
        ReadProperty<string>(message, "IsVisibleBindName").Should().Be(nameof(AppearanceEditorViewModel.IsNoEditableItemVisible));
    }

    private static void AssertNavigation(IGuiWidget row)
    {
        Walk(row).OfType<GuiComboBox<AppearanceEditorViewModel>>().Should().BeEmpty("the equipment slots are tabs on the Equipment editor");
        Walk(row).OfType<GuiLabel<AppearanceEditorViewModel>>().Should().BeEmpty("the empty-slot note sits beside the slot tabs");
        Walk(row).Should().NotContain(widget => widget.Id == "ae_outfits",
            "outfits are a tab of the editor, not a separate window");
        var outfitsTab = Walk(row).OfType<GuiToggles<AppearanceEditorViewModel>>()
            .Single(toggles => ReadProperty<string>(toggles, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsOutfitsVisible));
        ReadProperty<string>(outfitsTab, "SelectedValueBindName").Should().Be(nameof(AppearanceEditorViewModel.OutfitsTabToggleValue));
        Width(outfitsTab).Should().Be(AppearanceEditorDefinition.TabWidth);
    }

    [Test]
    public void TabPanelsHostNoNestedPartials()
    {
        foreach (var name in EditorPanels)
            Slots(_partials[name]).Should().BeEmpty($"{name} has no nested layout to swap");
    }

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    [TestCase(AppearanceEditorViewModel.EditorWeaponPartial)]
    public void EditorsFitTheFixedSizeWindow(string partialName)
    {
        var initial = _window.InitialGeometry;
        initial.Width.Should().Be(AppearanceEditorDefinition.EditorWidth);
        initial.Height.Should().Be(AppearanceEditorDefinition.EditorHeight);
        var preview = Walk(_partials[partialName]).Single(widget => widget.Id == "ae_part_preview");
        var contentRow = PathTo(_partials[partialName], preview).OfType<GuiRow<AppearanceEditorViewModel>>()
            .First(row => ReadProperty<string>(row, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.HasItemEquipped));
        var width = contentRow.Elements.Sum(MinimumWidth);
        width.Should().BeLessThanOrEqualTo(initial.Width - 40f,
            $"{partialName} must leave the window border and scrollbar allowance");
    }

    [Test]
    public void TheEditorIsNotResizable()
    {
        IsResizable(new AppearanceEditorDefinition()).Should().BeFalse(
            "a fixed size means a large saved size from the old layout never applies");
    }

    // ------------------------------------------------------------------ body map

    [TestCase(AppearanceEditorViewModel.EditorArmorPartial, true)]
    [TestCase(AppearanceEditorViewModel.EditorMainPartial, false)]
    public void BodyMapSlotsBindTheirPictureHighlightAndClick(string partialName, bool equipment)
    {
        var partial = _partials[partialName];
        var expected = Enum.GetValues<AppearanceEditorSlot>()
            .Where(slot => equipment || !ArmorOnlySlots.Contains(slot)).ToArray();
        expected.Should().HaveCount(equipment ? 21 : 15);

        var slots = Walk(partial).Where(widget => widget.Id?.StartsWith("ae_slot_", StringComparison.Ordinal) == true)
            .ToArray();
        slots.Select(widget => widget.Id).Should().BeEquivalentTo(expected.Select(slot => $"ae_slot_{slot}"));
        foreach (var slot in expected)
        {
            var image = $"Map{slot}Image";
            var selected = $"Map{slot}Selected";
            typeof(AppearanceEditorViewModel).GetProperty(image)!.PropertyType.Should().Be(typeof(string));
            typeof(AppearanceEditorViewModel).GetProperty(selected)!.PropertyType.Should().Be(typeof(bool));

            var button = slots.Single(widget => widget.Id == $"ae_slot_{slot}");
            button.Should().BeOfType<GuiButtonImage<AppearanceEditorViewModel>>();
            ReadProperty<string>(button, "ResrefBindName").Should().Be(image);
            ReadProperty<string>(button, "IsEncouragedBindName").Should().Be(selected);
            OutlineBindings(button).Should().Equal(new[] { selected },
                "the selected slot is shown by an outline, never by text");
            AssertSingleHandler(button, "click", nameof(AppearanceEditorViewModel.OnClickMapSlot),
                (typeof(AppearanceEditorSlot), slot));
            ReadProperty<string>(button, "Tooltip").Should().NotBeNullOrWhiteSpace();
        }

        if (!equipment)
        {
            foreach (var slot in ArmorOnlySlots)
                Walk(partial).Should().NotContain(widget => widget.Id == $"ae_slot_{slot}",
                    $"{slot} has no body part on the Appearance tab");
        }
        else
        {
            ReadProperty<string>(slots.Single(widget => widget.Id == "ae_slot_Head"), "Tooltip").Should().Be("Helmet");
        }
    }

    [Test]
    public void QuickOutfitRowIsOnlyOnTheEquipmentMap()
    {
        var armor = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        var combo = Walk(armor).Single(widget => widget.Id == "ae_quick_outfit");
        ReadProperty<string>(combo, "SelectedIndexBindName").Should().Be(nameof(AppearanceEditorViewModel.QuickOutfitValue));
        foreach (var (id, method, enabled) in new[]
                 {
                     ("ae_quick_wear", nameof(AppearanceEditorViewModel.OnClickQuickWear), nameof(AppearanceEditorViewModel.IsQuickWearEnabled)),
                     ("ae_quick_undo", nameof(AppearanceEditorViewModel.OnClickUndoOutfit), nameof(AppearanceEditorViewModel.IsUndoOutfitEnabled))
                 })
        {
            var button = Walk(armor).Single(widget => widget.Id == id);
            ReadProperty<string>(button, "IsEnabledBindName").Should().Be(enabled);
            AssertSingleHandler(button, "click", method);
        }

        foreach (var partial in new[] { AppearanceEditorViewModel.EditorMainPartial, AppearanceEditorViewModel.EditorWeaponPartial })
            Walk(_partials[partial]).Should().NotContain(widget => widget.Id == "ae_quick_outfit");
    }

    [Test]
    public void OutfitsTabWiresEveryWardrobeAction()
    {
        var outfits = _partials[AppearanceEditorViewModel.OutfitsPartial];
        foreach (var (id, method) in new[]
                 {
                     ("ae_outfit_slot", nameof(AppearanceEditorViewModel.OnClickOutfitSlot)),
                     ("ae_outfit_new", nameof(AppearanceEditorViewModel.OnClickNewOutfit)),
                     ("ae_outfit_delete", nameof(AppearanceEditorViewModel.OnClickDeleteOutfit)),
                     ("ae_outfit_rename", nameof(AppearanceEditorViewModel.OnClickRenameOutfit)),
                     ("ae_outfit_store", nameof(AppearanceEditorViewModel.OnClickStoreOutfit)),
                     ("ae_outfit_wear", nameof(AppearanceEditorViewModel.OnClickWearOutfit)),
                     ("ae_outfit_undo", nameof(AppearanceEditorViewModel.OnClickUndoOutfit))
                 })
            AssertSingleHandler(Walk(outfits).Single(widget => widget.Id == id), "click", method);

        Slots(outfits).Should().BeEmpty("the Outfits tab holds no part browser");
    }

    [Test]
    public void NewAndDeleteFitInsideTheOutfitList()
    {
        var outfits = _partials[AppearanceEditorViewModel.OutfitsPartial];
        var list = Walk(outfits).OfType<GuiGroup<AppearanceEditorViewModel>>()
            .Single(group => Width(group) > 0 && Walk(group).Any(widget => widget.Id == "ae_outfit_delete"));
        var buttons = new[] { "ae_outfit_new", "ae_outfit_delete" }
            .Select(id => Width(Walk(list).Single(widget => widget.Id == id))).ToArray();

        buttons.Should().AllSatisfy(width => width.Should().BePositive("an unsized button takes about 150 and overflows the list"));
        buttons.Sum().Should().BeLessThan(Width(list), "both buttons fit the list's border with room for padding");
    }

    [Test]
    public void CopyButtonsAreOnlyOnTheEquipmentMap()
    {
        var armor = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        foreach (var (id, method) in new[]
                 {
                     ("ae_copy_right", nameof(AppearanceEditorViewModel.OnClickCopyToRight)),
                     ("ae_copy_left", nameof(AppearanceEditorViewModel.OnClickCopyToLeft))
                 })
        {
            var button = Walk(armor).Single(widget => widget.Id == id);
            ReadProperty<string>(button, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsCopyEnabled));
            ReadProperty<float>(button, "Width").Should().Be(ReadProperty<float>(button, "Height"),
                "the copy buttons are square slots under the arms");
            ReadProperty<string>(button, "Resref").Should().Be(id == "ae_copy_right" ? "arrow_left" : "arrow_right",
                "each arrow points at the side it copies to");
            AssertSingleHandler(button, "click", method);
            Walk(_partials[AppearanceEditorViewModel.EditorMainPartial]).Should().NotContain(widget => widget.Id == id);
        }
    }

    // ------------------------------------------------------------------ palette

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    public void PaletteHas176CroppedButtonsWithoutHeaders(string partialName)
    {
        var palette = Walk(_partials[partialName]).Single(widget => widget.Id == "ae_color_palette");
        palette.Should().BeOfType<GuiGroup<AppearanceEditorViewModel>>();
        ReadProperty<string>(palette, "IsVisibleBindName").Should().Be(nameof(AppearanceEditorViewModel.IsColorPickerVisible));
        ReadProperty<NuiScrollbars>(palette, "Scrollbars").Should().Be(NuiScrollbars.None);
        palette.Events.Should().BeEmpty("each palette button supplies its own preset index");

        const float cellWidth = AppearanceEditorViewModel.PaletteCellWidth;
        const float cellHeight = AppearanceEditorViewModel.PaletteCellHeight;
        Width(palette).Should().Be(16 * cellWidth);
        palette.DeclaredHeight.Should().Be(11 * cellHeight);

        // The selection outline is one bound polyline drawn over the whole palette.
        var outline = DrawItems(palette).Should().ContainSingle().Which
            .Should().BeOfType<GuiDrawListItemPolyLine<AppearanceEditorViewModel>>().Which;
        ReadProperty<string>(outline, "PointsBindName").Should().Be(nameof(AppearanceEditorViewModel.PaletteSelectionPoints));
        ReadProperty<string>(outline, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsPaletteSelectionVisible));
        ReadProperty<bool>(outline, "IsFilled").Should().BeFalse();

        var rows = palette.Elements.Should().ContainSingle().Which.Elements;
        rows.Should().HaveCount(11, "eleven palette rows with no letter or number headers");
        rows.Should().OnlyContain(row => row.DeclaredMargin == 0f);

        var buttons = new List<GuiButton<AppearanceEditorViewModel>>();
        for (var y = 0; y < 11; y++)
        {
            var row = rows[y];
            row.DeclaredHeight.Should().Be(cellHeight);
            row.Elements.Should().HaveCount(16);
            buttons.AddRange(row.Elements.Cast<GuiButton<AppearanceEditorViewModel>>());
        }

        buttons.Should().HaveCount(176);
        var crops = new List<(float, float, float, float)>();
        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];
            button.Id.Should().Be("ae_palette_" + index);
            AssertSingleHandler(button, "click", nameof(AppearanceEditorViewModel.OnClickColorPalette), (typeof(int), index));
            ReadProperty<string>(button, "Tooltip").Should().Be(AppearancePreview.PaletteCode(index));
            ReadProperty<string>(button, "IsEncouragedBindName").Should().BeNullOrEmpty(
                "the palette selection is the shared outline, not 176 encouraged bindings");
            Width(button).Should().Be(cellWidth);
            button.DeclaredHeight.Should().Be(cellHeight);
            button.DeclaredMargin.Should().Be(0f);

            var image = DrawItems(button).Should().ContainSingle().Which
                .Should().BeOfType<GuiDrawListItemImage<AppearanceEditorViewModel>>().Which;
            ReadProperty<string>(image, "ResrefBindName").Should().Be(nameof(AppearanceEditorViewModel.ColorSheetResref),
                "every cell follows the selected skin, hair, tattoo or armor palette sheet");
            ReadProperty<string>(image, "DrawTextureRegionBindName").Should().BeNullOrEmpty();
            var crop = ReadProperty<GuiRectangle>(image, "DrawTextureRegion");
            var actual = (crop.X, crop.Y, crop.Width, crop.Height);
            actual.Should().Be((index % 16 * 16f + 3f, index / 16 * 16f + 4f, 9f, 7f),
                "each cell samples a 9x7 interior of its own 16x16 sheet cell, drawn at exactly 2x");
            (crop.X + crop.Width).Should().BeLessThanOrEqualTo(256f);
            (crop.Y + crop.Height).Should().BeLessThanOrEqualTo(176f);
            var position = ReadProperty<GuiRectangle>(image, "Position");
            (position.X + position.Width).Should().BeLessThanOrEqualTo(cellWidth);
            (position.Y + position.Height).Should().BeLessThanOrEqualTo(cellHeight);
            crops.Add(actual);
        }
        crops.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void WeaponEditorHasPartCategoriesButNoColours()
    {
        var weapon = _partials[AppearanceEditorViewModel.EditorWeaponPartial];
        var widgets = Walk(weapon).ToArray();
        var category = widgets.Single(widget => widget.Id == "ae_part_category");
        ReadProperty<string>(category, "TextBindName").Should().Be(nameof(AppearanceEditorViewModel.PartCategoryOptions));
        ReadProperty<string>(category, "IsToggledBindName").Should().Be(nameof(AppearanceEditorViewModel.PartCategorySelected));
        AssertSingleHandler(category, "click", nameof(AppearanceEditorViewModel.OnSelectPartCategory));
        widgets.OfType<GuiList<AppearanceEditorViewModel>>().Single();
        widgets.Should().NotContain(widget => widget.Id != null && (widget.Id == "ae_color_palette" ||
            widget.Id == "ae_tint_picker" || widget.Id.StartsWith("ae_palette_") || widget.Id.StartsWith("ae_slot_") ||
            widget.Id.Contains("_swatch_")));
        widgets.OfType<GuiColorPicker<AppearanceEditorViewModel>>().Should().BeEmpty();
    }

    // ------------------------------------------------------------------ swatches

    [Test]
    public void EquipmentSwatchesBindRegionTintCustomAndSelected()
    {
        var armor = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        foreach (var channel in Channels)
        {
            var texture = channel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2
                ? "gui_pal_armor01"
                : "gui_pal_tattoo";

            var global = Walk(armor).Single(widget => widget.Id == $"ae_base_swatch_{channel}");
            AssertSwatch(armor, global, $"Global{channel}", texture);
            AssertSingleHandler(global, "click", nameof(AppearanceEditorViewModel.OnClickBaseSwatch),
                (typeof(AppearanceArmorColor), channel));

            var part = Walk(armor).Single(widget => widget.Id == $"ae_part_swatch_{channel}");
            AssertSwatch(armor, part, $"SelectedPart{channel}", texture);
            part.Events.Keys.Should().BeEquivalentTo("click", "mousedown");
            AssertHandler(part.Events["click"], nameof(AppearanceEditorViewModel.OnClickPartSwatch),
                (typeof(AppearanceArmorColor), channel));
            AssertHandler(part.Events["mousedown"], nameof(AppearanceEditorViewModel.OnMouseDownPartSwatch),
                (typeof(AppearanceArmorColor), channel));
            PathTo(armor, part).Should().Contain(ancestor =>
                    ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsPartColorsVisible),
                "part colours only show while an armor part is selected");
        }

        var swatches = Walk(armor).Where(widget => widget.Id?.Contains("_swatch_") == true).ToArray();
        swatches.Should().HaveCount(12);
        Walk(armor).Should().NotContain(widget => widget.Id != null && widget.Id.StartsWith("ae_color_") &&
            widget.Id != "ae_color_palette", "the 120 per-target swatches were replaced by the selected-part row");
        Walk(armor).Should().NotContain(widget => widget.Id == "ae_body_swatch_Skin");
    }

    [Test]
    public void AppearanceSwatchesBindRegionTintCustomAndSelected()
    {
        var main = _partials[AppearanceEditorViewModel.EditorMainPartial];
        var expected = new[]
        {
            ("Skin", "gui_pal_skin"), ("Hair", "gui_pal_hair01"), ("Tattoo1", "gui_pal_tattoo"), ("Tattoo2", "gui_pal_tattoo")
        };
        for (var index = 0; index < expected.Length; index++)
        {
            var (name, texture) = expected[index];
            var swatch = Walk(main).Single(widget => widget.Id == $"ae_body_swatch_{name}");
            AssertSwatch(main, swatch, $"Body{name}", texture);
            AssertSingleHandler(swatch, "click", nameof(AppearanceEditorViewModel.OnClickBodySwatch), (typeof(int), index));
        }
        Walk(main).Where(widget => widget.Id?.Contains("_swatch_") == true).Should().HaveCount(4);
    }

    [Test]
    public void UseBaseSitsOnThePartColourRowAndClearsOnlyPartColours()
    {
        var partial = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        var useBase = Walk(partial).Single(widget => widget.Id == "ae_use_base");
        PathTo(partial, useBase).Should().Contain(ancestor =>
            ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsPartColorsVisible));
        ReadProperty<string>(useBase, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsUseBaseEnabled));
        AssertSingleHandler(useBase, "click", nameof(AppearanceEditorViewModel.OnClickUseBase));
        Walk(_partials[AppearanceEditorViewModel.EditorMainPartial]).Should().NotContain(widget => widget.Id == "ae_use_base",
            "body colours have no part colours to clear");
    }

    // ------------------------------------------------------------------ RGB picker

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    public void ColourPickerAndRgbFieldsBindTheCustomTint(string partialName)
    {
        var partial = _partials[partialName];
        var picker = Walk(partial).OfType<GuiColorPicker<AppearanceEditorViewModel>>().Should().ContainSingle().Which;
        picker.Id.Should().Be("ae_tint_picker");
        ReadProperty<string>(picker, "SelectedColorBindName").Should().Be(nameof(AppearanceEditorViewModel.SelectedTintColor));
        ReadProperty<string>(picker, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsCustomTintEditable));
        Width(picker).Should().Be(196f);
        picker.DeclaredHeight.Should().Be(100f);
        picker.Events.Keys.Should().BeEquivalentTo("mousedown", "mouseup");
        AssertHandler(picker.Events["mousedown"], nameof(AppearanceEditorViewModel.OnMouseDownTintPicker));
        AssertHandler(picker.Events["mouseup"], nameof(AppearanceEditorViewModel.OnMouseUpTintPicker));
        PathTo(partial, picker).Should().NotContain(widget => widget.Id == "ae_color_palette",
            "the picker sits below the palette, outside its fixed-size group");

        var pickerRow = PathTo(partial, picker).OfType<GuiRow<AppearanceEditorViewModel>>().Last();
        ReadProperty<string>(pickerRow, "IsVisibleBindName").Should().Be(nameof(AppearanceEditorViewModel.IsCustomTintAvailable));
        NaturalWidth(pickerRow).Should().BeLessThanOrEqualTo(AppearanceEditorDefinition.ColoursWidth - 16f,
            "the picker and R/G/B fields must fit the colour column");

        var fields = Walk(partial).OfType<GuiTextEdit<AppearanceEditorViewModel>>()
            .Where(field => PathTo(partial, field).Contains(pickerRow)).ToArray();
        fields.Select(field => ReadProperty<string>(field, "ValueBindName")).Should().Equal(
            nameof(AppearanceEditorViewModel.CustomTintRed), nameof(AppearanceEditorViewModel.CustomTintGreen),
            nameof(AppearanceEditorViewModel.CustomTintBlue));
        var channels = new[] { "R", "G", "B" };
        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            ReadProperty<int>(field, "MaxLength").Should().Be(3);
            ReadProperty<string>(field, "IsEnabledBindName").Should().Be(nameof(AppearanceEditorViewModel.IsCustomTintEditable));
            var fieldRow = PathTo(partial, field).OfType<GuiRow<AppearanceEditorViewModel>>().Last();
            ReadProperty<string>(fieldRow.Elements[0], "Text").Should().Be(channels[index]);
        }

        var title = Walk(partial).OfType<GuiLabel<AppearanceEditorViewModel>>()
            .Single(label => ReadProperty<string>(label, "Text") == "Custom Colour (RGB)");
        PathTo(partial, title).Should().Contain(ancestor =>
            ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsCustomTintAvailable));
    }

    // ------------------------------------------------------------------ part browser window

    [Test]
    public void ThePartBrowserOffersViewsPagerAndPartNumber()
    {
        var browser = AppearancePartBrowserDefinition.BuildBrowserColumn();
        var views = Walk(browser).Where(widget => widget.Id?.StartsWith("ae_view_") == true).ToArray();
        views.Select(view => view.Id).Should().Equal(
            Enum.GetValues<AppearancePreviewView>().Select(view => $"ae_view_{view}"));
        views.Select(view => ReadProperty<string>(view, "IsToggledBindName")).Should().Equal(
            nameof(AppearancePartBrowserViewModel.IsFrontViewSelected), nameof(AppearancePartBrowserViewModel.IsBackViewSelected),
            nameof(AppearancePartBrowserViewModel.IsAngleViewSelected));
        views.Should().OnlyContain(view => ReadProperty<string>(view, "IsVisibleBindName") ==
            nameof(AppearancePartBrowserViewModel.IsPreviewViewChoiceVisible), "weapon pages have only front pictures");
        for (var index = 0; index < views.Length; index++)
            AssertBrowserHandler(views[index], nameof(AppearancePartBrowserViewModel.OnClickView),
                (typeof(AppearancePreviewView), (AppearancePreviewView)index));

        var pager = Walk(browser).Single(widget => widget.GetType().Name.StartsWith("GuiComboBox"));
        ReadProperty<string>(pager, "OptionsBindName").Should().Be(nameof(AppearancePartBrowserViewModel.PageOptions));
        ReadProperty<string>(pager, "SelectedIndexBindName").Should().Be(nameof(AppearancePartBrowserViewModel.PageIndex));
        AssertBrowserHandler(Walk(browser).Single(widget => widget.Id == "ae_browser_previous"),
            nameof(AppearancePartBrowserViewModel.OnClickPrevious));
        AssertBrowserHandler(Walk(browser).Single(widget => widget.Id == "ae_browser_next"),
            nameof(AppearancePartBrowserViewModel.OnClickNext));
        AssertBrowserHandler(Walk(browser).Single(widget => widget.Id == "ae_browser_go"),
            nameof(AppearancePartBrowserViewModel.OnClickGo));
        var number = Walk(browser).Single(widget => widget.Id == "ae_browser_part_number");
        ReadProperty<string>(number, "ValueBindName").Should().Be(nameof(AppearancePartBrowserViewModel.PartNumberText));
        ReadProperty<string>(number, "Placeholder").Should().Be("Part #");
    }

    [Test]
    public void ThePartBrowserHasFiveByThreeBoundTiles()
    {
        var browser = AppearancePartBrowserDefinition.BuildBrowserColumn();
        var tiles = Walk(browser).Where(widget => widget.Id?.StartsWith("ae_tile_") == true).ToArray();
        tiles.Should().HaveCount(AppearancePartBrowserViewModel.PageSize);
        AppearancePartBrowserViewModel.Columns.Should().Be(5);
        AppearancePartBrowserViewModel.Rows.Should().Be(3);
        var tileRows = browser.Elements.Where(row => Walk(row).Any(widget => widget.Id?.StartsWith("ae_tile_") == true)).ToArray();
        tileRows.Should().HaveCount(AppearancePartBrowserViewModel.Rows);
        foreach (var row in tileRows)
            Walk(row).Count(widget => widget.Id?.StartsWith("ae_tile_") == true).Should().Be(AppearancePartBrowserViewModel.Columns);

        for (var tile = 0; tile < tiles.Length; tile++)
        {
            foreach (var (suffix, type) in new[]
                     {
                         ("Image", typeof(string)), ("Visible", typeof(bool)), ("Selected", typeof(bool)), ("Label", typeof(string))
                     })
                typeof(AppearancePartBrowserViewModel).GetProperty($"Tile{tile}{suffix}")!.PropertyType.Should().Be(type);

            var button = tiles[tile];
            button.Id.Should().Be($"ae_tile_{tile}");
            ReadProperty<string>(button, "ResrefBindName").Should().Be($"Tile{tile}Image");
            ReadProperty<string>(button, "IsVisibleBindName").Should().Be($"Tile{tile}Visible");
            ReadProperty<string>(button, "IsEncouragedBindName").Should().Be($"Tile{tile}Selected");
            OutlineBindings(button).Should().Equal($"Tile{tile}Selected");
            Width(button).Should().Be(AppearanceEditorDefinition.TileSize);
            button.DeclaredHeight.Should().Be(AppearanceEditorDefinition.TileSize);
            AssertBrowserHandler(button, nameof(AppearancePartBrowserViewModel.OnClickTile), (typeof(int), tile));
        }
        typeof(AppearancePartBrowserViewModel).GetProperty($"Tile{AppearancePartBrowserViewModel.PageSize}Image")
            .Should().BeNull("the browser binds exactly one page of tiles");

        NaturalWidth(browser).Should().BeLessThanOrEqualTo(AppearancePartBrowserDefinition.WindowWidth - 40f,
            "five pictures fit the browser window");
        NaturalHeight(browser).Should().BeLessThanOrEqualTo(AppearancePartBrowserDefinition.WindowHeight - 60f,
            "three rows and the pager fit below the title bar");
    }

    [Test]
    public void ThePartBrowserWindowIsAFixedSizeWithNoLayoutFindings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var window = new AppearancePartBrowserDefinition().BuildWindow();
        window.LayoutFindings.Should().BeEmpty();
        window.InitialGeometry.Width.Should().Be(AppearancePartBrowserDefinition.WindowWidth);
        window.InitialGeometry.Height.Should().Be(AppearancePartBrowserDefinition.WindowHeight);
        IsResizable(new AppearancePartBrowserDefinition()).Should().BeFalse();

        // A screen smaller than the window (low resolution, high GUI scale) scrolls the content.
        var authored = AuthoredWindow(new AppearancePartBrowserDefinition());
        var content = ((IReadOnlyDictionary<string, IGuiWidget>)authored.GetType().GetProperty("PartialViews")!
            .GetValue(authored)!)[WindowMain];
        var tile = Walk(content).Single(widget => widget.Id == "ae_tile_0");
        PathTo(content, tile).Should().Contain(widget => widget.GetType().Name.StartsWith("GuiGroup", StringComparison.Ordinal) &&
            ReadProperty<NuiScrollbars>(widget, "Scrollbars") == NuiScrollbars.Auto);
    }

    // ------------------------------------------------------------------ settings

    [Test]
    public void SettingsContentUsesClientWidthAndVerticalScrolling()
    {
        var settings = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.SettingsPartial);
        ReadProperty<NuiScrollbars>(settings, "Scrollbars").Should().Be(NuiScrollbars.Y);
        Walk(settings).Where(widget => widget is GuiGroup<AppearanceEditorViewModel> or GuiColumn<AppearanceEditorViewModel>)
            .Should().OnlyContain(widget => Width(widget) == 0f);
        Walk(settings).OfType<GuiRow<AppearanceEditorViewModel>>()
            .Should().OnlyContain(row => Width(row) == 0f && NaturalWidth(row) <= 256f,
                "fixed controls must leave the surrounding spacers free to follow the client width");
    }

    [Test]
    public void SettingsKeepScaleButtonsAvailableWithoutPlayerPreferenceControls()
    {
        var settings = _partials[AppearanceEditorViewModel.SettingsPartial];
        // DMs toggle their avatar's or possessed creature's helmet and cloak; only players save preferences.
        foreach (var widget in Walk(settings).OfType<GuiCheckBox<AppearanceEditorViewModel>>())
            PathTo(settings, widget).Should().Contain(ancestor =>
                ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsArmorDisplayVisible));
        PathTo(settings, Walk(settings).Single(widget => widget.Id == "ae_settings_save")).Should().Contain(ancestor =>
            ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsPlayerSettingsVisible));

        foreach (var id in new[] { "ae_height_increase", "ae_height_decrease", "ae_head_increase", "ae_head_decrease" })
        {
            var button = Walk(settings).Single(widget => widget.Id == id);
            PathTo(settings, button).Should().NotContain(ancestor =>
                ReadProperty<string>(ancestor, "IsVisibleBindName") == nameof(AppearanceEditorViewModel.IsPlayerSettingsVisible));
        }
        _window.LayoutFindings.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ helpers

    private static void AssertBrowserHandler(IGuiWidget widget, string method, params (Type Type, object Value)[] arguments)
    {
        widget.Events.Keys.Should().Equal(new[] { "click" }, $"{widget.Id} needs only its click event");
        var handler = widget.Events["click"];
        handler.Method.Name.Should().Be(method);
        handler.Method.DeclaringType.Should().Be(typeof(AppearancePartBrowserViewModel));
        handler.Arguments.Select(argument => (argument.Key, argument.Value))
            .Should().Equal(arguments.Select(argument => (argument.Type, argument.Value)));
    }

    private static void AssertSwatch(IGuiWidget root, IGuiWidget swatch, string name, string texture)
    {
        foreach (var (suffix, type) in new[]
                 {
                     ("Region", typeof(GuiRectangle)), ("Tint", typeof(GuiColor)), ("Custom", typeof(bool)),
                     ("Selected", typeof(bool))
                 })
            typeof(AppearanceEditorViewModel).GetProperty(name + suffix)!.PropertyType.Should().Be(type, name + suffix);
        typeof(AppearanceEditorViewModel).GetProperty(name + "Code").Should().BeNull(
            "the palette outline shows the selected colour, so swatches have no code labels");

        swatch.Should().BeOfType<GuiButton<AppearanceEditorViewModel>>();
        ReadProperty<string>(swatch, "IsEncouragedBindName").Should().Be(name + "Selected");
        var items = DrawItems(swatch).ToArray();
        items.Should().HaveCount(3, $"{name}: palette crop, custom RGB fill and selection outline");

        var image = items[0].Should().BeOfType<GuiDrawListItemImage<AppearanceEditorViewModel>>().Which;
        ReadProperty<string>(image, "Resref").Should().Be(texture);
        ReadProperty<string>(image, "DrawTextureRegionBindName").Should().Be(name + "Region");

        var fill = items[1].Should().BeOfType<GuiDrawListItemPolyLine<AppearanceEditorViewModel>>().Which;
        ReadProperty<string>(fill, "ColorBindName").Should().Be(name + "Tint");
        ReadProperty<string>(fill, "IsEnabledBindName").Should().Be(name + "Custom");
        ReadProperty<bool>(fill, "IsFilled").Should().BeTrue();

        var outline = items[2].Should().BeOfType<GuiDrawListItemPolyLine<AppearanceEditorViewModel>>().Which;
        ReadProperty<string>(outline, "IsEnabledBindName").Should().Be(name + "Selected");
        ReadProperty<bool>(outline, "IsFilled").Should().BeFalse();

        var row = PathTo(root, swatch).OfType<GuiRow<AppearanceEditorViewModel>>().Last();
        row.Elements.OfType<GuiLabel<AppearanceEditorViewModel>>()
            .Should().NotContain(label => ReadProperty<string>(label, "TextBindName") != null, "swatch rows carry no palette code labels");
    }

    private static string[] OutlineBindings(IGuiWidget widget) => DrawItems(widget)
        .Where(item => item.GetType().Name.StartsWith("GuiDrawListItemPolyLine", StringComparison.Ordinal))
        .Select(line => ReadProperty<string>(line, "IsEnabledBindName")).ToArray();

    private static IEnumerable<object> DrawItems(IGuiWidget widget) =>
        ReadProperty<System.Collections.IEnumerable>(widget, "DrawLists").Cast<object>()
            .SelectMany(list => ReadProperty<System.Collections.IEnumerable>(list, "DrawItems").Cast<object>());

    private static void AssertSingleHandler(IGuiWidget widget, string eventName, string method,
        params (Type Type, object Value)[] arguments)
    {
        widget.Events.Keys.Should().Equal(new[] { eventName }, $"{widget.Id} needs only its {eventName} event");
        AssertHandler(widget.Events[eventName], method, arguments);
    }

    private static void AssertHandler(GuiMethodDetail handler, string method, params (Type Type, object Value)[] arguments)
    {
        handler.Method.Name.Should().Be(method);
        handler.Method.DeclaringType.Should().Be(typeof(AppearanceEditorViewModel));
        handler.Arguments.Select(argument => (argument.Key, argument.Value))
            .Should().Equal(arguments.Select(argument => (argument.Type, argument.Value)));
    }

    private static IReadOnlyDictionary<string, string> EventSnapshot(IGuiWidget root) => Walk(root)
        .SelectMany(widget => widget.Events.Select(entry => new KeyValuePair<string, string>(
            widget.Id + "/" + entry.Key,
            entry.Value.Method.DeclaringType!.FullName + "." + entry.Value.Method + "(" +
            string.Join(",", entry.Value.Arguments.Select(argument => argument.Key.FullName + ":" +
                Convert.ToString(argument.Value, System.Globalization.CultureInfo.InvariantCulture))) + ")")))
        .Where(entry => !Guid.TryParse(entry.Key.Split('/')[0], out _))
        .ToDictionary(entry => entry.Key, entry => entry.Value);

    private static IReadOnlyDictionary<string, string> BindingSnapshot(IGuiWidget root) =>
        BoundObjects(root, "root").SelectMany(entry => Properties(entry.Value)
            .Where(property => property.PropertyType == typeof(string) &&
                property.Name.EndsWith("BindName", StringComparison.Ordinal))
            .Select(property => new KeyValuePair<string, string>(entry.Path + "." + property.Name,
                (string)property.GetValue(entry.Value))))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

    private static IEnumerable<(string Path, object Value)> BoundObjects(IGuiWidget widget, string path)
    {
        yield return (path, widget);
        var drawLists = ReadProperty<System.Collections.IEnumerable>(widget, "DrawLists").Cast<object>().ToArray();
        for (var index = 0; index < drawLists.Length; index++)
        {
            var drawPath = path + "/draw/" + index;
            yield return (drawPath, drawLists[index]);
            var items = ReadProperty<System.Collections.IEnumerable>(drawLists[index], "DrawItems").Cast<object>().ToArray();
            for (var item = 0; item < items.Length; item++)
                yield return (drawPath + "/" + item, items[item]);
        }
        for (var index = 0; index < widget.Elements.Count; index++)
        foreach (var child in BoundObjects(widget.Elements[index], path + "/" + index))
            yield return child;
    }

    private static IEnumerable<PropertyInfo> Properties(object target)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        foreach (var property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            yield return property;
    }

    // Partial-view slots are empty groups with an id.
    private static IEnumerable<string> Slots(IGuiWidget widget) => Walk(widget)
        .OfType<GuiGroup<AppearanceEditorViewModel>>()
        .Where(group => group.Elements.Count == 0 && !string.IsNullOrEmpty(group.Id)).Select(group => group.Id);

    private static IEnumerable<IGuiWidget> Walk(IGuiWidget widget) =>
        new[] { widget }.Concat(widget.Elements.SelectMany(Walk));

    private static IEnumerable<IGuiWidget> PathTo(IGuiWidget root, IGuiWidget target)
    {
        if (ReferenceEquals(root, target))
            return new[] { root };
        foreach (var child in root.Elements)
        {
            var tail = PathTo(child, target).ToArray();
            if (tail.Length > 0)
                return new[] { root }.Concat(tail);
        }
        return Array.Empty<IGuiWidget>();
    }

    private static float Width(IGuiWidget widget) => ReadProperty<float>(widget, "Width");

    // Lower bounds from actual fixed dimensions; no guessed engine margins.
    private static float MinimumWidth(IGuiWidget widget) => Math.Max(Width(widget), NaturalWidth(widget));
    private static float NaturalWidth(IGuiWidget widget) => IsWidget(widget, typeof(GuiRow<>))
        ? widget.Elements.Sum(MinimumWidth)
        : widget.Elements.Select(MinimumWidth).DefaultIfEmpty(0f).Max();
    private static float MinimumHeight(IGuiWidget widget) => Math.Max(widget.DeclaredHeight, NaturalHeight(widget));
    private static float NaturalHeight(IGuiWidget widget) => IsWidget(widget, typeof(GuiColumn<>))
        ? widget.Elements.Sum(MinimumHeight)
        : widget.Elements.Select(MinimumHeight).DefaultIfEmpty(0f).Max();

    /// <summary>Builds a window definition and reads whether its authored window can be resized.</summary>
    private static bool IsResizable(IGuiWindowDefinition definition) =>
        ReadProperty<bool>(AuthoredWindow(definition), "IsResizable");

    /// <summary>Builds a window definition and returns the window its builder authored.</summary>
    private static object AuthoredWindow(IGuiWindowDefinition definition)
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        definition.BuildWindow();
        var builder = definition.GetType()
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return builder.GetType()
            .GetField("_activeWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static bool IsWidget(IGuiWidget widget, Type genericType) =>
        widget.GetType().IsGenericType && widget.GetType().GetGenericTypeDefinition() == genericType;

    private static T ReadProperty<T>(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property != null)
                return (T)property.GetValue(target)!;
        }
        throw new InvalidOperationException($"Missing widget metadata {target.GetType().Name}.{name}");
    }
}
