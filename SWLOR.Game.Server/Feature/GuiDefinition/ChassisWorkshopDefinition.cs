using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The Chassis Workshop: an astromech's chassis, paint and size, laid out like the Appearance Editor it
    /// stands in for. The Chassis tab shows every chassis as a picture beside the paint swatches, the worn
    /// chassis's picture, the palette and the RGB picker; Settings holds the size. Every change applies at once.
    /// </summary>
    public class ChassisWorkshopDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ChassisWorkshopViewModel> _builder = new();

        public const float WindowWidth = AppearanceEditorDefinition.EditorWidth;
        public const float WindowHeight = AppearanceEditorDefinition.EditorHeight;
        /// <summary>The chassis pictures take the place and width of the Appearance Editor's body map.</summary>
        public const float ChassisPanelWidth = AppearanceEditorDefinition.MapWidth;
        public const int TileColumns = 2;
        private const float TileSize = AppearanceEditorDefinition.TileSize;

        // The sizes below are the Appearance Editor's, so the two windows read as one.
        private const float PreviewColumnWidth = AppearanceEditorDefinition.PreviewColumnWidth;
        private const float SwatchesColumnWidth = 196f;
        private const float PreviewArrowWidth = 30f;
        private const float PreviewViewButtonWidth = 56f;
        private const float SwatchWidth = 52f;
        private const float SwatchHeight = 24f;
        private const float SwatchNameWidth = 72f;
        private const float PickerWidth = 196f;
        private const float PickerHeight = 100f;
        private const float SettingsButtonWidth = 128f;
        private const float ScaleLabelWidth = SettingsButtonWidth * 2;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ChassisWorkshop)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Chassis Workshop")
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ChassisWorkshopViewModel.ChassisPartial, Flush(BuildChassis))
                .DefinePartialView(ChassisWorkshopViewModel.SettingsPartial, Flush(BuildSettings))
                .AddStandardLayout(layout =>
                {
                    layout.SetTabPanelHeight(AppearanceEditorDefinition.TabPanelHeight);
                    layout.SetCompactContent();
                    layout.AddTabRow(BuildNavigation);
                    layout.SetContentPartialElement(ChassisWorkshopViewModel.ContentElement);
                });

            return _builder.Build();
        }

        /// <summary>Builds one panel as the window defines it (used by tests).</summary>
        public static GuiGroup<ChassisWorkshopViewModel> BuildPanel(string partialName)
        {
            var panel = new GuiGroup<ChassisWorkshopViewModel>().SetShowBorder(false);
            switch (partialName)
            {
                case ChassisWorkshopViewModel.ChassisPartial: BuildChassis(panel); break;
                case ChassisWorkshopViewModel.SettingsPartial: BuildSettings(panel); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(partialName), partialName, "Unknown chassis workshop panel.");
            }
            return panel;
        }

        /// <summary>Drops the partial's own margin so its panels start right under the tabs.</summary>
        private static Action<GuiGroup<ChassisWorkshopViewModel>> Flush(Action<GuiGroup<ChassisWorkshopViewModel>> build) =>
            partial =>
            {
                partial.SetMargin(0f);
                build(partial);
            };

        private static void BuildNavigation(GuiRow<ChassisWorkshopViewModel> row)
        {
            row.AddSpacer();
            row.AddToggles()
                .AddOption("Chassis")
                .AddOption("Settings")
                .BindSelectedValue(model => model.TabToggleValue)
                .SetWidth(AppearanceEditorDefinition.TabWidth * 2)
                .SetHeight(28f);
            row.AddSpacer();
        }

        // ---------------------------------------------------------------- chassis
        private static void BuildChassis(GuiGroup<ChassisWorkshopViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(BuildChassisPictures).SetWidth(ChassisPanelWidth);
                    row.AddGroup(BuildColours);
                });
            });
        }

        /// <summary>Every chassis as a picture with its name; the worn one is outlined.</summary>
        private static void BuildChassisPictures(GuiGroup<ChassisWorkshopViewModel> group)
        {
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Y);
            group.AddColumn(col =>
            {
                var count = AstromechChassisCatalog.All.Count;
                for (var first = 0; first < count; first += TileColumns)
                {
                    var start = first;
                    col.AddRow(tiles =>
                    {
                        tiles.AddSpacer();
                        for (var tile = start; tile < Math.Min(start + TileColumns, count); tile++)
                            AddTile(tiles, tile);
                        tiles.AddSpacer();
                    });
                }

                col.AddRow(fill => fill.AddSpacer());
            });
        }

        private static void AddTile(GuiRow<ChassisWorkshopViewModel> tiles, int tile)
        {
            var name = AstromechChassisCatalog.All[tile].Name;
            tiles.AddColumn(cell =>
            {
                cell.AddRow(imageRow =>
                {
                    var selected = Bind<bool>($"Tile{tile}Selected");
                    imageRow.AddButtonImage()
                        .SetId($"cw_tile_{tile}")
                        .BindImageResref(Bind<string>($"Tile{tile}Image"))
                        .BindIsEncouraged(selected)
                        .SetTooltip($"Wear the {name}.")
                        .SetWidth(TileSize)
                        .SetHeight(TileSize)
                        .SetMargin(2f)
                        .BindOnClicked(model => model.OnClickTile(tile))
                        .AddDrawList(list => list.AddPolyLine(outline => AddRectangle(outline, TileSize, TileSize, 1.5f)
                            .BindIsEnabled(selected)
                            .SetColor(AppearanceEditorDefinition.Highlight)
                            .SetLineThickness(3f)));
                });
                cell.AddRow(labelRow =>
                {
                    labelRow.AddLabel()
                        .SetText(name)
                        .SetWidth(TileSize + 10f)
                        .SetHeight(18f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });
            }).SetWidth(TileSize + 10f);
        }

        // ---------------------------------------------------------------- colours
        /// <summary>The paint swatches beside the worn chassis's picture, then the palette and RGB picker.</summary>
        private static void BuildColours(GuiGroup<ChassisWorkshopViewModel> group)
        {
            // A window narrower than the swatches and palette scrolls them sideways instead of cutting them off.
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Auto);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    // Fixed widths keep the picture beside the swatches in a wider window.
                    row.AddColumn(swatches =>
                    {
                        BuildPaintSwatches(swatches);
                        swatches.AddRow(fill => fill.AddSpacer());
                    }).SetWidth(SwatchesColumnWidth);
                    row.AddColumn(AddChassisPreview).SetWidth(PreviewColumnWidth);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddGroup(BuildColorPalette).SetId("cw_color_palette");
                    row.AddSpacer();
                });
                BuildCustomColour(col);
                col.AddRow(fill => fill.AddSpacer());
            });
        }

        /// <summary>
        /// The worn chassis's picture with previous and next chassis buttons and a button that turns the
        /// pictures between front, back and angle views.
        /// </summary>
        private static void AddChassisPreview(GuiColumn<ChassisWorkshopViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButtonImage()
                    .SetId("cw_preview")
                    .BindImageResref(model => model.PreviewImage)
                    .SetTooltip("Turns the pictures: front, back or angle.")
                    .SetWidth(TileSize)
                    .SetHeight(TileSize)
                    .SetMargin(2f)
                    .BindOnClicked(model => model.OnClickView());
                row.AddSpacer();
            });
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.PreviewLabel)
                    .SetHeight(18f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetId("cw_chassis_previous")
                    .SetText("<")
                    .SetTooltip("Previous chassis")
                    .SetWidth(PreviewArrowWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickPreviousChassis());
                row.AddButton()
                    .SetId("cw_view")
                    .BindText(model => model.PreviewViewText)
                    .SetTooltip("Turns the pictures: front, back or angle.")
                    .SetWidth(PreviewViewButtonWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickView());
                row.AddButton()
                    .SetId("cw_chassis_next")
                    .SetText(">")
                    .SetTooltip("Next chassis")
                    .SetWidth(PreviewArrowWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickNextChassis());
                row.AddSpacer();
            });
        }

        /// <summary>Primary and secondary, then trim, each under its name, like the body colours.</summary>
        private static void BuildPaintSwatches(GuiColumn<ChassisWorkshopViewModel> col)
        {
            var swatches = new[]
            {
                (AstromechPaintZone.Primary, "Primary"), (AstromechPaintZone.Secondary, "Secondary"),
                (AstromechPaintZone.Trim, "Trim")
            };
            for (var first = 0; first < swatches.Length; first += 2)
            {
                var pair = swatches[first..Math.Min(first + 2, swatches.Length)];
                col.AddRow(row =>
                {
                    foreach (var (_, name) in pair)
                    {
                        row.AddLabel().SetText(name).SetWidth(SwatchNameWidth).SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                    }
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    foreach (var (zone, name) in pair)
                    {
                        // Centre the swatch (and its 2 px margins) under its name.
                        var gap = (SwatchNameWidth - SwatchWidth - 4f) / 2f;
                        row.AddSpacer().SetWidth(gap);
                        AddSwatch(row, zone, name);
                        row.AddSpacer().SetWidth(gap);
                    }
                    row.AddSpacer();
                });
            }
        }

        /// <summary>A swatch button: the paint as a fill, and a highlight outline when it is the zone being painted.</summary>
        private static void AddSwatch(GuiRow<ChassisWorkshopViewModel> row, AstromechPaintZone zone, string name)
        {
            var selected = Bind<bool>(name + "Selected");
            row.AddButton()
                .SetId($"cw_swatch_{name}")
                .SetText("")
                .SetTooltip("Paints this part of the chassis. Right-click to return it to the chassis's own paint.")
                .SetWidth(SwatchWidth)
                .SetHeight(SwatchHeight)
                .SetMargin(2f)
                .BindIsEncouraged(selected)
                .BindOnClicked(model => model.OnClickSwatch(zone))
                .BindOnMouseDown(model => model.OnMouseDownSwatch(zone))
                .AddDrawList(list =>
                {
                    list.AddPolyLine(fill => fill
                        .BindColor(Bind<GuiColor>(name + "Tint"))
                        .SetIsFilled(true)
                        .AddPoint(3f, 3f)
                        .AddPoint(SwatchWidth - 3f, 3f)
                        .AddPoint(SwatchWidth - 3f, SwatchHeight - 3f)
                        .AddPoint(3f, SwatchHeight - 3f));
                    list.AddPolyLine(outline => AddRectangle(outline, SwatchWidth, SwatchHeight, 1f)
                        .BindIsEnabled(selected)
                        .SetColor(AppearanceEditorDefinition.Highlight)
                        .SetLineThickness(2f));
                });
        }

        // ---------------------------------------------------------------- palette
        private static void BuildColorPalette(GuiGroup<ChassisWorkshopViewModel> group)
        {
            const int columns = AstromechWorkshop.PaletteColumns;
            const int rows = TintMapMaterialRegistry.PaletteColorCount / columns;
            var cellWidth = AppearanceEditorViewModel.PaletteCellWidth;
            var cellHeight = AppearanceEditorViewModel.PaletteCellHeight;
            group.SetShowBorder(false).SetScrollbars(NuiScrollbars.None).SetPadding(0f).SetMargin(0f);
            group.SetWidth(AppearanceEditorViewModel.PaletteHeaderWidth + columns * cellWidth);
            group.SetHeight(AppearanceEditorViewModel.PaletteHeaderHeight + rows * cellHeight);
            // One outline over the palette cell the paint matches; its points follow the paint.
            group.AddDrawList(list => list.AddPolyLine(outline => outline
                .BindIsEnabled(model => model.IsPaletteSelectionVisible)
                .SetColor(AppearanceEditorDefinition.Highlight)
                .SetLineThickness(2f)
                .BindPoints(model => model.PaletteSelectionPoints)));
            group.AddColumn(col =>
            {
                col.AddRow(header =>
                {
                    header.SetHeight(AppearanceEditorViewModel.PaletteHeaderHeight).SetMargin(0f);
                    header.AddSpacer().SetWidth(AppearanceEditorViewModel.PaletteHeaderWidth);
                    for (var column = 0; column < columns; column++)
                    {
                        header.AddLabel()
                            .SetText(((char)('A' + column)).ToString())
                            .SetWidth(cellWidth)
                            .SetHeight(AppearanceEditorViewModel.PaletteHeaderHeight)
                            .SetMargin(0f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                    }
                });

                for (var y = 0; y < rows; ++y)
                {
                    var rowIndex = y;
                    col.AddRow(uiRow =>
                    {
                        uiRow.SetHeight(cellHeight).SetMargin(0f);
                        uiRow.AddLabel()
                            .SetText((rowIndex + 1).ToString())
                            .SetWidth(AppearanceEditorViewModel.PaletteHeaderWidth)
                            .SetHeight(cellHeight)
                            .SetMargin(0f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        for (var x = 0; x < columns; ++x)
                        {
                            var paletteIndex = rowIndex * columns + x;
                            // Sample 9x7 texels of the unfiltered sheet and draw them at exactly 2x
                            // (18x14 inside the 20x16 cell) so gradient swatches stay even.
                            var region = new GuiRectangle(
                                x * AppearanceEditorViewModel.ColorSize + 3,
                                rowIndex * AppearanceEditorViewModel.ColorSize + 4,
                                9, 7);
                            uiRow.AddButton()
                                .SetId("cw_palette_" + paletteIndex)
                                .SetText("")
                                .SetTooltip(AppearancePreview.PaletteCode(paletteIndex))
                                .SetWidth(cellWidth)
                                .SetHeight(cellHeight)
                                .SetMargin(0f)
                                .AddDrawList(drawList => drawList.AddImage(image =>
                                {
                                    image.BindResref(model => model.ColorSheetResref);
                                    image.SetPosition(1f, 1f, cellWidth - 2f, cellHeight - 2f);
                                    image.SetAspect(NuiAspect.Stretch);
                                    image.SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    image.SetVerticalAlign(NuiVerticalAlign.Top);
                                    image.SetDrawTextureRegion(region);
                                }))
                                .BindOnClicked(model => model.OnClickPalette(paletteIndex));
                        }
                    });
                }
            });
        }

        private static void BuildCustomColour(GuiColumn<ChassisWorkshopViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel().SetText("Custom Colour (RGB)").SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            col.AddRow(row =>
            {
                row.AddColorPicker()
                    .SetId("cw_tint_picker")
                    .BindSelectedColor(model => model.SelectedColor)
                    .SetTooltip("Apply an RGB color.")
                    .BindOnMouseDown(model => model.OnMouseDownPicker())
                    .BindOnMouseUp(model => model.OnMouseUpPicker())
                    .SetWidth(PickerWidth)
                    .SetHeight(PickerHeight);
                row.AddColumn(fields =>
                {
                    AddRgbField(fields, "R", model => model.CustomRed);
                    AddRgbField(fields, "G", model => model.CustomGreen);
                    AddRgbField(fields, "B", model => model.CustomBlue);
                }).SetWidth(96f);
                row.AddSpacer();
            });
        }

        private static void AddRgbField(GuiColumn<ChassisWorkshopViewModel> col, string channel,
            Expression<Func<ChassisWorkshopViewModel, string>> binding)
        {
            col.AddRow(row =>
            {
                row.AddLabel().SetText(channel).SetWidth(18f).SetHeight(30f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddTextEdit()
                    .SetId($"cw_rgb_{channel}")
                    .BindValue(binding)
                    .SetTooltip("Apply an RGB color.")
                    .SetMaxLength(3)
                    .SetWidth(58f)
                    .SetHeight(30f);
                row.AddSpacer();
            });
        }

        // ---------------------------------------------------------------- settings
        /// <summary>The size, stepped like the Appearance Editor's height; it is kept as soon as it changes.</summary>
        private static void BuildSettings(GuiGroup<ChassisWorkshopViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.Y);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddLabel()
                        .BindText(model => model.ScaleText)
                        .SetWidth(ScaleLabelWidth)
                        .SetHeight(26f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetId("cw_size_increase")
                        .SetText("Increase Size")
                        .SetHeight(32f)
                        .SetWidth(SettingsButtonWidth)
                        .BindOnClicked(model => model.OnClickLarger());
                    row.AddButton()
                        .SetId("cw_size_decrease")
                        .SetText("Decrease Size")
                        .SetHeight(32f)
                        .SetWidth(SettingsButtonWidth)
                        .BindOnClicked(model => model.OnClickSmaller());
                    row.AddSpacer();
                });
            });
        }

        // ---------------------------------------------------------------- helpers
        private static GuiDrawListItemPolyLine<ChassisWorkshopViewModel> AddRectangle(
            GuiDrawListItemPolyLine<ChassisWorkshopViewModel> line, float width, float height, float inset) =>
            line.AddPoint(inset, inset)
                .AddPoint(width - inset, inset)
                .AddPoint(width - inset, height - inset)
                .AddPoint(inset, height - inset)
                .AddPoint(inset, inset);

        private static Expression<Func<ChassisWorkshopViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ChassisWorkshopViewModel), "model");
            return Expression.Lambda<Func<ChassisWorkshopViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }
    }
}
