using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using System.Linq.Expressions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Appearance Editor: body map | colours (swatches, palette, RGB picker) | part picture browser.
    /// The selected slot, swatch, palette cell and picture are shown with NUI highlights.
    /// </summary>
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        public const float MapWidth = 280f;
        public const float ColoursWidth = 372f;
        public const float WeaponCategoryWidth = 200f;
        public const float TileSize = 96f; // pictures are 96x96; drawn 1:1 so they stay sharp
        private const float SlotSize = 52f;
        private const float SwatchWidth = 36f;
        private const float SwatchHeight = 24f;
        private const float PickerWidth = 196f;
        private const float PickerHeight = 100f;
        /// <summary>The 28-high tabs plus the panel's padding; at 32 the tab row scrolls.</summary>
        public const float TabPanelHeight = 36f;
        /// <summary>Width of one tab option; NUI keeps about this much per option whatever the control's width.</summary>
        public const float TabWidth = 150f;
        /// <summary>The Equipment tab's Armor / Main Hand / Off Hand row, measured in the client.</summary>
        public const float EquipmentTabsHeight = 36f;
        private const float NoItemLabelWidth = 130f;
        private const float OutfitButtonWidth = 64f;
        private const float OutfitListWidth = 280f;
        /// <summary>New and Delete share the list's width; unsized buttons take about 150 each and overflow it.</summary>
        public const float OutfitListButtonWidth = (OutfitListWidth - 32f) / 2;
        private const float OutfitStoreButtonWidth = 170f;

        /// <summary>
        /// Editor size in a window too small for one column of pictures. The editor keeps this
        /// size and the window's content area scrolls, so every panel stays reachable.
        /// </summary>
        public const float CompactBrowserWidth = TileSize + 64f;

        private const float SideColumnWidth = 58f;
        private const float PairColumnWidth = 58f;
        private const float CenterColumnWidth = 120f;

        private static readonly GuiColor Highlight = new(255, 196, 40);

        /// <summary>Builds one editor panel as the window defines it (used by tests).</summary>
        public static GuiGroup<AppearanceEditorViewModel> BuildEditorPanel(string partialName)
        {
            var definition = new AppearanceEditorDefinition();
            var panel = new GuiGroup<AppearanceEditorViewModel>().SetShowBorder(false);
            switch (partialName)
            {
                case AppearanceEditorViewModel.EditorMainPartial: definition.BuildAppearanceEditor(panel); break;
                case AppearanceEditorViewModel.EditorArmorPartial: definition.BuildEquipmentEditor(panel); break;
                case AppearanceEditorViewModel.EditorWeaponPartial: definition.BuildWeaponEditor(panel); break;
                case AppearanceEditorViewModel.EditorMainCompactPartial: definition.BuildAppearanceEditor(panel, true); break;
                case AppearanceEditorViewModel.EditorArmorCompactPartial: definition.BuildEquipmentEditor(panel, true); break;
                case AppearanceEditorViewModel.EditorWeaponCompactPartial: definition.BuildWeaponEditor(panel, true); break;
                case AppearanceEditorViewModel.SettingsPartial: definition.BuildSettings(panel); break;
                case AppearanceEditorViewModel.OutfitsPartial: definition.BuildOutfits(panel); break;
                default:
                    for (var columns = 1; columns <= AppearanceEditorViewModel.MaxBrowserColumns; columns++)
                    {
                        for (var rows = 1; rows <= AppearanceEditorViewModel.MaxBrowserRows; rows++)
                        {
                            if (partialName == AppearanceEditorViewModel.BrowserGrid(columns, rows))
                            {
                                definition.BuildBrowserGrid(panel, columns, rows);
                                return panel;
                            }
                        }
                    }
                    throw new System.ArgumentOutOfRangeException(nameof(partialName), partialName, "Unknown appearance editor panel.");
            }
            return panel;
        }

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.AppearanceEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 1240f, 720f)
                .SetTitle("Appearance Editor")
                .BindOnClosed(model => model.OnCloseWindow())
                .DefinePartialView(AppearanceEditorViewModel.EditorMainPartial, Flush(BuildAppearanceEditor))
                .DefinePartialView(AppearanceEditorViewModel.EditorArmorPartial, Flush(BuildEquipmentEditor))
                .DefinePartialView(AppearanceEditorViewModel.EditorWeaponPartial, Flush(BuildWeaponEditor))
                .DefinePartialView(AppearanceEditorViewModel.EditorMainCompactPartial, partial => BuildAppearanceEditor(partial, true))
                .DefinePartialView(AppearanceEditorViewModel.EditorArmorCompactPartial, partial => BuildEquipmentEditor(partial, true))
                .DefinePartialView(AppearanceEditorViewModel.EditorWeaponCompactPartial, partial => BuildWeaponEditor(partial, true))
                .DefinePartialView(AppearanceEditorViewModel.SettingsPartial, Flush(BuildSettings))
                .DefinePartialView(AppearanceEditorViewModel.OutfitsPartial, Flush(BuildOutfits))
                .AddStandardLayout(layout =>
                {
                    layout.SetTabPanelHeight(TabPanelHeight);
                    layout.SetCompactContent();
                    layout.AddTabRow(BuildNavigation);
                    layout.SetContentPartialElement(AppearanceEditorViewModel.MainPartialElement);
                });
            for (var columns = 1; columns <= AppearanceEditorViewModel.MaxBrowserColumns; columns++)
            {
                for (var rows = 1; rows <= AppearanceEditorViewModel.MaxBrowserRows; rows++)
                {
                    var (c, r) = (columns, rows);
                    window.DefinePartialView(AppearanceEditorViewModel.BrowserGrid(c, r), partial => BuildBrowserGrid(partial, c, r));
                }
            }

            return _builder.Build();
        }

        /// <summary>Drops the editor partial's own margin so its panels start right under the tabs.</summary>
        private static Action<GuiGroup<AppearanceEditorViewModel>> Flush(Action<GuiGroup<AppearanceEditorViewModel>> build) =>
            partial =>
            {
                partial.SetMargin(0f);
                build(partial);
            };

        /// <summary>The window's tabs. Equipment adds its own Armor / Main Hand / Off Hand row.</summary>
        private void BuildNavigation(GuiRow<AppearanceEditorViewModel> row)
        {
            row.AddSpacer();
            row.AddToggles()
                .AddOption("Appearance")
                .AddOption("Equipment")
                .BindSelectedValue(model => model.EditorTabToggleValue)
                .SetWidth(TabWidth * 2)
                .SetHeight(28f);
            row.AddToggles()
                .AddOption("Wardrobe")
                .BindSelectedValue(model => model.OutfitsTabToggleValue)
                .BindIsVisible(model => model.IsOutfitsVisible)
                .SetWidth(TabWidth)
                .SetHeight(28f);
            row.AddToggles()
                .AddOption("Settings")
                .BindSelectedValue(model => model.SettingsTabToggleValue)
                .BindIsVisible(model => model.IsSettingsVisible)
                .SetWidth(TabWidth)
                .SetHeight(28f);
            row.AddSpacer();
        }

        /// <summary>
        /// Armor, Main Hand and Off Hand as a second tab row on the Equipment editors, with the
        /// note shown when that slot holds nothing to edit. The row stays when the slot is empty
        /// so another slot can be picked. Helmets and cloaks are picked on the armor body map.
        /// </summary>
        private static void AddEquipmentTabs(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddToggles()
                    .AddOption("Armor")
                    .AddOption("Main Hand")
                    .AddOption("Off Hand")
                    .BindSelectedValue(model => model.EquipmentTypeValue)
                    .SetWidth(TabWidth * 3)
                    .SetHeight(28f);
                row.AddLabel()
                    .SetText("Nothing to edit.")
                    .BindIsVisible(model => model.IsNoEditableItemVisible)
                    .SetWidth(NoItemLabelWidth)
                    .SetHeight(28f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddSpacer();
            });
        }

        // ---------------------------------------------------------------- editors
        /// <summary>
        /// Compact editors (windows too small for one column of pictures) scroll sideways to reach
        /// every panel. Each panel scrolls vertically on its own: NUI stops drawing the nested
        /// picture grid once its top is scrolled out of an outer group.
        /// </summary>
        private static void SizeEditor(GuiGroup<AppearanceEditorViewModel> partial, bool compact) =>
            partial.SetScrollbars(compact ? NuiScrollbars.X : NuiScrollbars.None);

        private void AddBrowser(GuiRow<AppearanceEditorViewModel> row, bool compact)
        {
            var browser = row.AddGroup(group => BuildBrowser(group, compact));
            if (compact)
                browser.SetWidth(CompactBrowserWidth);
        }

        private void BuildAppearanceEditor(GuiGroup<AppearanceEditorViewModel> partial) => BuildAppearanceEditor(partial, false);

        private void BuildAppearanceEditor(GuiGroup<AppearanceEditorViewModel> partial, bool compact)
        {
            SizeEditor(partial, compact);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);
                    row.AddGroup(map => BuildBodyMap(map, equipment: false)).SetWidth(MapWidth);
                    row.AddGroup(colours => BuildColours(colours, BuildBodySwatches)).SetWidth(ColoursWidth);
                    AddBrowser(row, compact);
                });
            });
        }

        private void BuildEquipmentEditor(GuiGroup<AppearanceEditorViewModel> partial) => BuildEquipmentEditor(partial, false);

        private void BuildEquipmentEditor(GuiGroup<AppearanceEditorViewModel> partial, bool compact)
        {
            SizeEditor(partial, compact);
            partial.AddColumn(col =>
            {
                AddEquipmentTabs(col);
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);
                    row.AddGroup(map => BuildBodyMap(map, equipment: true)).SetWidth(MapWidth);
                    row.AddGroup(colours => BuildColours(colours, BuildItemSwatches)).SetWidth(ColoursWidth);
                    AddBrowser(row, compact);
                });
            });
        }

        private void BuildWeaponEditor(GuiGroup<AppearanceEditorViewModel> partial) => BuildWeaponEditor(partial, false);

        private void BuildWeaponEditor(GuiGroup<AppearanceEditorViewModel> partial, bool compact)
        {
            SizeEditor(partial, compact);
            partial.AddColumn(col =>
            {
                AddEquipmentTabs(col);
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);
                    row.AddGroup(group =>
                    {
                        group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Auto);
                        group.AddColumn(list => list.AddRow(listRow =>
                        {
                            listRow.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .SetId("ae_part_category")
                                        .BindText(model => model.PartCategoryOptions)
                                        .BindIsToggled(model => model.PartCategorySelected)
                                        .BindOnClicked(model => model.OnSelectPartCategory());
                                });
                            })
                                .BindRowCount(model => model.PartCategoryOptions)
                                .SetHeight(160f);
                        }));
                    }).SetWidth(WeaponCategoryWidth);
                    AddBrowser(row, compact);
                });
            });
        }

        // ---------------------------------------------------------------- body map
        private static readonly string[] SlotNames =
        {
            "Head", "Neck", "Chest", "Belt", "Pelvis", "Robe", "Cloak", "Right Shoulder", "Left Shoulder",
            "Right Bicep", "Left Bicep", "Right Forearm", "Left Forearm", "Right Hand", "Left Hand",
            "Right Thigh", "Left Thigh", "Right Shin", "Left Shin", "Right Foot", "Left Foot"
        };

        private void AddSlot(GuiColumn<AppearanceEditorViewModel> col, AppearanceEditorSlot slot, bool equipment, float size)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                var image = Bind<string>($"Map{slot}Image");
                var selected = Bind<bool>($"Map{slot}Selected");
                var name = slot == AppearanceEditorSlot.Head && equipment ? "Helmet" : SlotNames[(int)slot];
                AddOutline(row.AddButtonImage()
                        .SetId($"ae_slot_{slot}")
                        .BindImageResref(image)
                        .SetTooltip(name)
                        .SetWidth(size)
                        .SetHeight(size)
                        .SetMargin(2f)
                        .BindIsEncouraged(selected)
                        .BindOnClicked(model => model.OnClickMapSlot(slot)),
                    size, size, selected, 3f);
                row.AddSpacer();
            });
        }

        private void BuildBodyMap(GuiGroup<AppearanceEditorViewModel> group, bool equipment)
        {
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Y);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    if (equipment)
                        AddEquipmentMap(row);
                    else
                        AddBodyMap(row);
                    row.AddSpacer();
                });

                if (equipment)
                    AddQuickOutfitRows(col);

                col.AddRow(fill => fill.AddSpacer());
            });
        }

        private void AddEquipmentMap(GuiRow<AppearanceEditorViewModel> row)
        {
            row.AddColumn(side =>
            {
                AddSlot(side, AppearanceEditorSlot.Robe, true, SlotSize);
                side.AddRow(gap => gap.AddSpacer().SetHeight(12f));
                AddSlot(side, AppearanceEditorSlot.RightShoulder, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.RightBicep, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.RightForearm, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.RightHand, true, SlotSize);
                side.AddRow(gap => gap.AddSpacer().SetHeight(24f));
                AddCopyButton(side, "ae_copy_left", "arrow_right",
                    "Copy to Left: copies the right side's parts and colours to the left side.",
                    model => model.OnClickCopyToLeft());
                side.AddRow(fill => fill.AddSpacer());
            }).SetWidth(SideColumnWidth);
            row.AddColumn(center =>
            {
                AddSlot(center, AppearanceEditorSlot.Head, true, SlotSize);
                AddSlot(center, AppearanceEditorSlot.Neck, true, 40f);
                AddSlot(center, AppearanceEditorSlot.Chest, true, 78f);
                AddSlot(center, AppearanceEditorSlot.Belt, true, 44f);
                AddSlot(center, AppearanceEditorSlot.Pelvis, true, 60f);
                AddPair(center, AppearanceEditorSlot.RightThigh, AppearanceEditorSlot.LeftThigh, true);
                AddPair(center, AppearanceEditorSlot.RightShin, AppearanceEditorSlot.LeftShin, true);
                AddPair(center, AppearanceEditorSlot.RightFoot, AppearanceEditorSlot.LeftFoot, true);
                center.AddRow(fill => fill.AddSpacer());
            }).SetWidth(CenterColumnWidth);
            row.AddColumn(side =>
            {
                AddSlot(side, AppearanceEditorSlot.Cloak, true, SlotSize);
                side.AddRow(gap => gap.AddSpacer().SetHeight(12f));
                AddSlot(side, AppearanceEditorSlot.LeftShoulder, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.LeftBicep, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.LeftForearm, true, SlotSize);
                AddSlot(side, AppearanceEditorSlot.LeftHand, true, SlotSize);
                side.AddRow(gap => gap.AddSpacer().SetHeight(24f));
                AddCopyButton(side, "ae_copy_right", "arrow_left",
                    "Copy to Right: copies the left side's parts and colours to the right side.",
                    model => model.OnClickCopyToRight());
                side.AddRow(fill => fill.AddSpacer());
            }).SetWidth(SideColumnWidth);
        }

        /// <summary>
        /// Body parts only, packed together: the arms start level with the chest and nothing is
        /// reserved for the armor-only slots (neck, belt, robe, cloak, shoulders).
        /// </summary>
        private void AddBodyMap(GuiRow<AppearanceEditorViewModel> row)
        {
            void Arm(GuiColumn<AppearanceEditorViewModel> side, AppearanceEditorSlot bicep,
                AppearanceEditorSlot forearm, AppearanceEditorSlot hand)
            {
                side.AddRow(gap => gap.AddSpacer().SetHeight(SlotSize + 4f));
                AddSlot(side, bicep, false, SlotSize);
                AddSlot(side, forearm, false, SlotSize);
                AddSlot(side, hand, false, SlotSize);
                side.AddRow(fill => fill.AddSpacer());
            }

            row.AddColumn(side => Arm(side, AppearanceEditorSlot.RightBicep, AppearanceEditorSlot.RightForearm,
                AppearanceEditorSlot.RightHand)).SetWidth(SideColumnWidth);
            row.AddColumn(center =>
            {
                AddSlot(center, AppearanceEditorSlot.Head, false, SlotSize);
                AddSlot(center, AppearanceEditorSlot.Chest, false, 78f);
                AddSlot(center, AppearanceEditorSlot.Pelvis, false, 60f);
                AddPair(center, AppearanceEditorSlot.RightThigh, AppearanceEditorSlot.LeftThigh, false);
                AddPair(center, AppearanceEditorSlot.RightShin, AppearanceEditorSlot.LeftShin, false);
                AddPair(center, AppearanceEditorSlot.RightFoot, AppearanceEditorSlot.LeftFoot, false);
                center.AddRow(fill => fill.AddSpacer());
            }).SetWidth(CenterColumnWidth);
            row.AddColumn(side => Arm(side, AppearanceEditorSlot.LeftBicep, AppearanceEditorSlot.LeftForearm,
                AppearanceEditorSlot.LeftHand)).SetWidth(SideColumnWidth);
        }

        /// <summary>A square copy button under one arm; its arrow points at the side it copies to.</summary>
        private static void AddCopyButton(GuiColumn<AppearanceEditorViewModel> col, string id, string arrow,
            string tooltip, Expression<System.Func<AppearanceEditorViewModel, System.Action>> onClick)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButtonImage()
                    .SetId(id)
                    .SetImageResref(arrow)
                    .SetTooltip(tooltip)
                    .SetWidth(SlotSize)
                    .SetHeight(SlotSize)
                    .SetMargin(2f)
                    .BindIsEnabled(model => model.IsCopyEnabled)
                    .BindOnClicked(onClick);
                row.AddSpacer();
            });
        }

        private void AddPair(GuiColumn<AppearanceEditorViewModel> col, AppearanceEditorSlot right, AppearanceEditorSlot left, bool equipment)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddColumn(c => AddSlot(c, right, equipment, SlotSize)).SetWidth(PairColumnWidth);
                row.AddColumn(c => AddSlot(c, left, equipment, SlotSize)).SetWidth(PairColumnWidth);
                row.AddSpacer();
            });
        }

        // ---------------------------------------------------------------- colours
        private void BuildColours(GuiGroup<AppearanceEditorViewModel> group,
            System.Action<GuiColumn<AppearanceEditorViewModel>> buildSwatches)
        {
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Y);
            group.AddColumn(col =>
            {
                buildSwatches(col);
                col.AddRow(row =>
                {
                    row.AddGroup(BuildColorPalette)
                        .SetId("ae_color_palette")
                        .BindIsVisible(model => model.IsColorPickerVisible);
                    row.AddSpacer();
                });
                BuildCustomTintEditor(col);
                col.AddRow(fill => fill.AddSpacer());
            });
        }

        private const float SwatchRowLabelWidth = 56f;
        private const float SwatchCodeWidth = 44f;

        /// <summary>
        /// Two sections: the item's own colours ("Outfit", "Helmet", "Cloak"), then the selected
        /// armor part's colours, which override the outfit colour on that part only.
        /// </summary>
        private void BuildItemSwatches(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.BaseColorsTitle)
                    .SetTooltip("Colours for the whole item.")
                    .SetWidth(SwatchRowLabelWidth)
                    .SetHeight(20f);
                foreach (var label in new[] { "Leather", "Cloth", "Metal" })
                {
                    row.AddLabel().SetText(label).SetWidth(SwatchWidth + 4f + SwatchCodeWidth).SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                }
                row.AddSpacer();
            });
            AddItemSwatchRow(col, "1", "Global", AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1,
                AppearanceArmorColor.Metal1, partRow: false);
            AddItemSwatchRow(col, "2", "Global", AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2,
                AppearanceArmorColor.Metal2, partRow: false);
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsPartColorsVisible);
                row.AddLabel()
                    .BindText(model => model.SelectedPartColorsTitle)
                    .SetTooltip("Colours for this part only. A swatch marked Outfit uses the outfit colour.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(SwatchHeight);
                row.AddButton()
                    .SetId("ae_use_base")
                    .SetText("Use Outfit")
                    .SetTooltip("Puts the selected part colour back to the outfit colour.")
                    .BindIsEnabled(model => model.IsUseBaseEnabled)
                    .SetWidth(90f)
                    .SetHeight(SwatchHeight)
                    .BindOnClicked(model => model.OnClickUseBase());
            });
            AddItemSwatchRow(col, "1", "SelectedPart", AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1,
                AppearanceArmorColor.Metal1, partRow: true);
            AddItemSwatchRow(col, "2", "SelectedPart", AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2,
                AppearanceArmorColor.Metal2, partRow: true);
        }

        private void AddItemSwatchRow(GuiColumn<AppearanceEditorViewModel> col, string label, string prefix,
            AppearanceArmorColor leather, AppearanceArmorColor cloth, AppearanceArmorColor metal, bool partRow)
        {
            col.AddRow(row =>
            {
                if (partRow)
                    row.BindIsVisible(model => model.IsPartColorsVisible);
                row.AddLabel().SetText(label).SetWidth(SwatchRowLabelWidth).SetHeight(SwatchHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
                foreach (var channel in new[] { leather, cloth, metal })
                {
                    var texture = channel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2
                        ? "gui_pal_armor01"
                        : "gui_pal_tattoo";
                    var name = $"{prefix}{channel}";
                    var copy = channel;
                    var button = AddSwatch(row, texture, name);
                    if (partRow)
                    {
                        button.SetId($"ae_part_swatch_{channel}")
                            .BindOnClicked(model => model.OnClickPartSwatch(copy))
                            .BindOnMouseDown(model => model.OnMouseDownPartSwatch(copy))
                            .SetTooltip("Colours only this part. Right-click to go back to the outfit colour.");
                    }
                    else
                    {
                        button.SetId($"ae_base_swatch_{channel}")
                            .SetTooltip("Colours the whole item. Parts marked Outfit use this colour.")
                            .BindOnClicked(model => model.OnClickBaseSwatch(copy));
                    }
                    row.AddLabel().BindText(Bind<string>(name + "Code")).SetWidth(SwatchCodeWidth).SetHeight(SwatchHeight);
                }
                row.AddSpacer();
            });
        }

        private void BuildBodySwatches(GuiColumn<AppearanceEditorViewModel> col)
        {
            var swatches = new[]
            {
                ("Skin", "Skin", "gui_pal_skin"), ("Hair", "Hair", "gui_pal_hair01"),
                ("Tattoo 1", "Tattoo1", "gui_pal_tattoo"), ("Tattoo 2", "Tattoo2", "gui_pal_tattoo")
            };
            col.AddRow(row =>
            {
                foreach (var (label, _, _) in swatches)
                {
                    row.AddLabel().SetText(label).SetWidth(84f).SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                }
                row.AddSpacer();
            });
            col.AddRow(row =>
            {
                for (var index = 0; index < swatches.Length; index++)
                {
                    var (_, name, texture) = swatches[index];
                    var copy = index;
                    row.AddSpacer().SetWidth(4f);
                    AddSwatch(row, texture, "Body" + name)
                        .SetId($"ae_body_swatch_{name}")
                        .BindOnClicked(model => model.OnClickBodySwatch(copy));
                    row.AddLabel().BindText(Bind<string>($"Body{name}Code")).SetWidth(40f).SetHeight(SwatchHeight);
                }
                row.AddSpacer();
            });
        }

        /// <summary>
        /// A swatch button: the palette crop, the exact RGB fill when custom, and a highlight outline
        /// when it is the colour being edited.
        /// </summary>
        private GuiButton<AppearanceEditorViewModel> AddSwatch(GuiRow<AppearanceEditorViewModel> row, string texture, string name)
        {
            var selected = Bind<bool>(name + "Selected");
            var button = row.AddButton()
                .SetText("")
                .SetWidth(SwatchWidth)
                .SetHeight(SwatchHeight)
                .SetMargin(2f)
                .BindIsEncouraged(selected)
                .AddDrawList(list =>
                {
                    list.AddImage(image => image
                        .SetResref(texture)
                        .SetPosition(3f, 3f, SwatchWidth - 6f, SwatchHeight - 6f)
                        .SetAspect(NuiAspect.Stretch)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Top)
                        .BindDrawTextureRegion(Bind<GuiRectangle>(name + "Region")));
                    list.AddPolyLine(fill => fill
                        .BindIsEnabled(Bind<bool>(name + "Custom"))
                        .BindColor(Bind<GuiColor>(name + "Tint"))
                        .SetIsFilled(true)
                        .AddPoint(3f, 3f)
                        .AddPoint(SwatchWidth - 3f, 3f)
                        .AddPoint(SwatchWidth - 3f, SwatchHeight - 3f)
                        .AddPoint(3f, SwatchHeight - 3f));
                    AddOutlineItems(list, SwatchWidth, SwatchHeight, selected, 2f);
                });
            return button;
        }

        // ---------------------------------------------------------------- palette
        private void BuildColorPalette(GuiGroup<AppearanceEditorViewModel> group)
        {
            const int columns = 16;
            const int rows = TintMapMaterialRegistry.PaletteColorCount / columns;
            var cellWidth = AppearanceEditorViewModel.PaletteCellWidth;
            var cellHeight = AppearanceEditorViewModel.PaletteCellHeight;
            group.SetShowBorder(false).SetScrollbars(NuiScrollbars.None).SetPadding(0f).SetMargin(0f);
            group.SetWidth(AppearanceEditorViewModel.PaletteHeaderWidth + columns * cellWidth);
            group.SetHeight(AppearanceEditorViewModel.PaletteHeaderHeight + rows * cellHeight);
            // One outline over the selected palette cell; its points follow the selected colour.
            group.AddDrawList(list => list.AddPolyLine(outline => outline
                .BindIsEnabled(model => model.IsPaletteSelectionVisible)
                .SetColor(Highlight)
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
                            CreateFilledButton(
                                    uiRow,
                                    null,
                                    null,
                                    cellWidth,
                                    cellHeight,
                                    model => model.OnClickColorPalette(paletteIndex),
                                    region,
                                    model => model.ColorSheetResref)
                                .SetId("ae_palette_" + paletteIndex)
                                .SetTooltip(AppearancePreview.PaletteCode(paletteIndex));
                        }
                    });
                }
            });
        }

        private GuiButton<AppearanceEditorViewModel> CreateFilledButton(
            GuiRow<AppearanceEditorViewModel> row,
            string texture,
            Expression<System.Func<AppearanceEditorViewModel, GuiRectangle>> regionBind,
            float width,
            float height,
            Expression<System.Func<AppearanceEditorViewModel, System.Action>> onClickBind,
            GuiRectangle staticRegion,
            Expression<System.Func<AppearanceEditorViewModel, string>> textureBinding)
        {
            return row.AddButton()
                .SetText("")
                .SetWidth(width)
                .SetHeight(height)
                .SetMargin(0f)
                .AddDrawList(drawList =>
                {
                    drawList.AddImage(image =>
                    {
                        if (textureBinding != null)
                            image.BindResref(textureBinding);
                        else
                            image.SetResref(texture);
                        image.SetPosition(1f, 1f, width - 2f, height - 2f);
                        image.SetAspect(NuiAspect.Stretch);
                        image.SetHorizontalAlign(NuiHorizontalAlign.Left);
                        image.SetVerticalAlign(NuiVerticalAlign.Top);
                        if (regionBind != null)
                            image.BindDrawTextureRegion(regionBind);
                        if (staticRegion != null)
                            image.SetDrawTextureRegion(staticRegion);
                    });
                })
                .BindOnClicked(onClickBind);
        }

        private void BuildCustomTintEditor(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsCustomTintAvailable);
                row.AddLabel().SetText("Custom Colour (RGB)").SetHeight(20f);
            });
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsCustomTintAvailable);
                row.AddColorPicker()
                    .SetId("ae_tint_picker")
                    .BindSelectedColor(model => model.SelectedTintColor)
                    .BindIsEnabled(model => model.IsCustomTintEditable)
                    .BindTooltip(model => model.CustomTintTooltip)
                    .BindOnMouseDown(model => model.OnMouseDownTintPicker())
                    .BindOnMouseUp(model => model.OnMouseUpTintPicker())
                    .SetWidth(PickerWidth)
                    .SetHeight(PickerHeight);
                row.AddColumn(fields =>
                {
                    AddRgbField(fields, "R", model => model.CustomTintRed);
                    AddRgbField(fields, "G", model => model.CustomTintGreen);
                    AddRgbField(fields, "B", model => model.CustomTintBlue);
                }).SetWidth(96f);
                row.AddSpacer();
            });
        }

        private static void AddRgbField(GuiColumn<AppearanceEditorViewModel> col, string channel,
            Expression<System.Func<AppearanceEditorViewModel, string>> binding)
        {
            col.AddRow(row =>
            {
                row.AddLabel().SetText(channel).SetWidth(18f).SetHeight(30f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddTextEdit()
                    .BindValue(binding)
                    .BindIsEnabled(model => model.IsCustomTintEditable)
                    .BindTooltip(model => model.CustomTintTooltip)
                    .SetMaxLength(3)
                    .SetWidth(58f)
                    .SetHeight(30f);
                row.AddSpacer();
            });
        }

        // ---------------------------------------------------------------- browser
        private void BuildBrowser(GuiGroup<AppearanceEditorViewModel> group) => BuildBrowser(group, false);

        /// <summary>
        /// The grid partial holds the view toggles, tiles and pager, sized for the window. A compact
        /// browser holds a one-picture grid directly and scrolls it: NUI stops drawing a nested
        /// grid group once it is scrolled.
        /// </summary>
        private void BuildBrowser(GuiGroup<AppearanceEditorViewModel> group, bool compact)
        {
            if (compact)
            {
                BuildBrowserGrid(group, 1, 1);
                group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Y);
                return;
            }
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col => col.AddRow(row => row.AddPartialView(AppearanceEditorViewModel.BrowserGridElement)));
        }

        private void BuildBrowserGrid(GuiGroup<AppearanceEditorViewModel> partial, int columns, int rows)
        {
            partial.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                // Toggle buttons size to their width; a tab bar keeps ~150px per option.
                var views = new (string Text, AppearancePreviewView View, string Bind)[]
                {
                    ("Front", AppearancePreviewView.Front, nameof(AppearanceEditorViewModel.IsFrontViewSelected)),
                    ("Back", AppearancePreviewView.Back, nameof(AppearanceEditorViewModel.IsBackViewSelected)),
                    ("Angle", AppearancePreviewView.Angle, nameof(AppearanceEditorViewModel.IsAngleViewSelected))
                };
                void AddViewButton(GuiRow<AppearanceEditorViewModel> row, (string Text, AppearancePreviewView View, string Bind) view, float width)
                {
                    row.AddToggleButton()
                        .SetId($"ae_view_{view.View}")
                        .SetText(view.Text)
                        .BindIsToggled(Bind<bool>(view.Bind))
                        .BindIsVisible(model => model.IsPreviewViewChoiceVisible)
                        .SetWidth(width)
                        .SetHeight(28f)
                        .BindOnClicked(model => model.OnClickPreviewView(view.View));
                }
                if (columns == 1)
                {
                    foreach (var view in views)
                        col.AddRow(row => AddViewButton(row, view, TileSize));
                }
                else
                {
                    col.AddRow(row =>
                    {
                        foreach (var view in views)
                            AddViewButton(row, view, columns == 2 ? 64f : 80f);
                        row.AddSpacer();
                    });
                }

                for (var row = 0; row < rows; row++)
                {
                    var first = row * columns;
                    col.AddRow(tiles =>
                    {
                        tiles.AddSpacer();
                        for (var column = 0; column < columns; column++)
                        {
                            var tile = first + column;
                            tiles.AddColumn(cell =>
                            {
                                cell.AddRow(imageRow =>
                                {
                                    var selected = Bind<bool>($"Tile{tile}Selected");
                                    AddOutline(imageRow.AddButtonImage()
                                            .SetId($"ae_tile_{tile}")
                                            .BindImageResref(Bind<string>($"Tile{tile}Image"))
                                            .BindIsVisible(Bind<bool>($"Tile{tile}Visible"))
                                            .BindIsEncouraged(selected)
                                            .SetWidth(TileSize)
                                            .SetHeight(TileSize)
                                            .SetMargin(2f)
                                            .BindOnClicked(model => model.OnClickTile(tile)),
                                        TileSize, TileSize, selected, 3f);
                                });
                                cell.AddRow(labelRow =>
                                {
                                    labelRow.AddLabel()
                                        .BindText(Bind<string>($"Tile{tile}Label"))
                                        .BindIsVisible(Bind<bool>($"Tile{tile}Visible"))
                                        .SetWidth(TileSize)
                                        .SetHeight(18f)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                                });
                            }).SetWidth(TileSize + 10f);
                        }
                        tiles.AddSpacer();
                    });
                }

                col.AddRow(fill => fill.AddSpacer());
                BuildPager(col, columns);
            });
        }

        private static void BuildPager(GuiColumn<AppearanceEditorViewModel> col, int columns)
        {
            void PageButtons(GuiRow<AppearanceEditorViewModel> row, float comboWidth)
            {
                row.AddButton().SetId("ae_browser_previous").SetText("<").SetWidth(30f).SetHeight(30f)
                    .BindOnClicked(model => model.OnClickBrowserPrevious());
                if (comboWidth > 0)
                {
                    row.AddComboBox()
                        .BindOptions(model => model.BrowserPageOptions)
                        .BindSelectedIndex(model => model.BrowserPageIndex)
                        .SetWidth(comboWidth)
                        .SetHeight(30f);
                }
                row.AddButton().SetId("ae_browser_next").SetText(">").SetWidth(30f).SetHeight(30f)
                    .BindOnClicked(model => model.OnClickBrowserNext());
            }

            void PartNumber(GuiRow<AppearanceEditorViewModel> row)
            {
                row.AddTextEdit()
                    .SetId("ae_browser_part_number")
                    .SetPlaceholder("Part #")
                    .SetMaxLength(5)
                    .BindValue(model => model.BrowserPartNumberText)
                    .SetWidth(56f)
                    .SetHeight(30f);
                row.AddButton().SetId("ae_browser_go").SetText("Go").SetWidth(40f).SetHeight(30f)
                    .SetTooltip("Selects the part with the number you typed.")
                    .BindOnClicked(model => model.OnClickBrowserGo());
            }

            void Combo(GuiRow<AppearanceEditorViewModel> row, float width) => row.AddComboBox()
                .BindOptions(model => model.BrowserPageOptions)
                .BindSelectedIndex(model => model.BrowserPageIndex)
                .SetWidth(width)
                .SetHeight(30f);

            if (columns >= 3)
            {
                col.AddRow(row =>
                {
                    PageButtons(row, 140f);
                    row.AddSpacer();
                    PartNumber(row);
                });
            }
            else if (columns == 2)
            {
                col.AddRow(row =>
                {
                    PageButtons(row, 126f);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    PartNumber(row);
                });
            }
            else
            {
                col.AddRow(row =>
                {
                    PageButtons(row, 0f);
                    row.AddSpacer();
                });
                col.AddRow(row => Combo(row, TileSize));
                col.AddRow(PartNumber);
            }
        }

        // ---------------------------------------------------------------- highlights
        private static TWidget AddOutline<TWidget>(TWidget widget, float width, float height,
            Expression<System.Func<AppearanceEditorViewModel, bool>> selected, float thickness)
            where TWidget : GuiWidget<AppearanceEditorViewModel, TWidget>
        {
            widget.AddDrawList(list => AddOutlineItems(list, width, height, selected, thickness));
            return widget;
        }

        private static void AddOutlineItems(GuiDrawList<AppearanceEditorViewModel> list, float width, float height,
            Expression<System.Func<AppearanceEditorViewModel, bool>> selected, float thickness)
        {
            var inset = thickness / 2f;
            list.AddPolyLine(outline => AddRectangle(outline, width, height, inset)
                .BindIsEnabled(selected)
                .SetColor(Highlight)
                .SetLineThickness(thickness));
        }

        private static GuiDrawListItemPolyLine<AppearanceEditorViewModel> AddRectangle(
            GuiDrawListItemPolyLine<AppearanceEditorViewModel> line, float width, float height, float inset) =>
            line.AddPoint(inset, inset)
                .AddPoint(width - inset, inset)
                .AddPoint(width - inset, height - inset)
                .AddPoint(inset, height - inset)
                .AddPoint(inset, inset);

        private static Expression<System.Func<AppearanceEditorViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(AppearanceEditorViewModel), "model");
            return Expression.Lambda<System.Func<AppearanceEditorViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }

        // ---------------------------------------------------------------- outfits
        /// <summary>
        /// Quick wear under the equipment body map: pick a saved outfit and wear it, or undo the
        /// last change. It uses the map's own free space, so the window does not grow.
        /// </summary>
        private static void AddQuickOutfitRows(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsOutfitsVisible);
                row.AddComboBox()
                    .SetId("ae_quick_outfit")
                    .BindOptions(model => model.QuickOutfitOptions)
                    .BindSelectedIndex(model => model.QuickOutfitValue)
                    .BindIsEnabled(model => model.IsQuickWearEnabled)
                    .SetTooltip("Saved outfits")
                    .SetHeight(32f);
            });
            // Wear and Undo sit under the picker, which takes the map's full width.
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsOutfitsVisible);
                row.AddSpacer();
                row.AddButton()
                    .SetId("ae_quick_wear")
                    .SetText("Wear")
                    .SetTooltip("Wear the chosen outfit on your clothes and helmet.")
                    .BindIsEnabled(model => model.IsQuickWearEnabled)
                    .BindOnClicked(model => model.OnClickQuickWear())
                    .SetWidth(OutfitButtonWidth)
                    .SetHeight(32f);
                row.AddButton()
                    .SetId("ae_quick_undo")
                    .SetText("Undo")
                    .SetTooltip("Undo: restores the look you wore before the last outfit change.")
                    .BindIsEnabled(model => model.IsUndoOutfitEnabled)
                    .BindOnClicked(model => model.OnClickUndoOutfit())
                    .SetWidth(OutfitButtonWidth)
                    .SetHeight(32f);
                row.AddSpacer();
            });
        }

        /// <summary>
        /// The wardrobe: a fixed list of saved outfits beside details that take the rest of the
        /// window, like the editors' part browser.
        /// </summary>
        private void BuildOutfits(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(slots =>
                    {
                        slots.SetShowBorder(true).SetScrollbars(NuiScrollbars.None);
                        slots.AddColumn(list =>
                        {
                            list.AddRow(header => header.AddLabel()
                                .BindText(model => model.WardrobeCapacityText)
                                .SetHeight(26f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle));
                            list.AddRow(listRow => listRow.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .SetId("ae_outfit_slot")
                                            .BindText(model => model.OutfitSlotNames)
                                            .BindIsToggled(model => model.OutfitSlotToggles)
                                            .BindOnClicked(model => model.OnClickOutfitSlot());
                                    });
                                })
                                .BindRowCount(model => model.OutfitSlotNames));
                            list.AddRow(buttons =>
                            {
                                buttons.AddButton()
                                    .SetId("ae_outfit_new")
                                    .SetText("New")
                                    .BindOnClicked(model => model.OnClickNewOutfit())
                                    .SetWidth(OutfitListButtonWidth)
                                    .SetHeight(32f);
                                buttons.AddButton()
                                    .SetId("ae_outfit_delete")
                                    .SetText("Delete")
                                    .BindIsEnabled(model => model.IsOutfitSelected)
                                    .BindOnClicked(model => model.OnClickDeleteOutfit())
                                    .SetWidth(OutfitListButtonWidth)
                                    .SetHeight(32f);
                            });
                        });
                    }).SetWidth(OutfitListWidth);

                    row.AddGroup(details =>
                    {
                        details.SetShowBorder(true).SetScrollbars(NuiScrollbars.None);
                        details.AddColumn(detail =>
                        {
                            detail.AddRow(nameRow =>
                            {
                                nameRow.BindIsVisible(model => model.IsOutfitSelected);
                                nameRow.AddTextEdit()
                                    .SetId("ae_outfit_name")
                                    .SetPlaceholder("Name")
                                    .BindValue(model => model.OutfitName)
                                    .SetMaxLength(Wardrobe.MaxNameLength)
                                    .SetHeight(32f);
                                nameRow.AddButton()
                                    .SetId("ae_outfit_rename")
                                    .SetText("Rename")
                                    .BindIsEnabled(model => model.IsOutfitRenameEnabled)
                                    .BindOnClicked(model => model.OnClickRenameOutfit())
                                    .SetWidth(90f)
                                    .SetHeight(32f);
                            });
                            detail.AddRow(textRow =>
                            {
                                textRow.BindIsVisible(model => model.IsOutfitSelected);
                                textRow.AddText().BindText(model => model.OutfitDetails);
                            });
                            detail.AddRow(actions =>
                            {
                                actions.BindIsVisible(model => model.IsOutfitSelected);
                                actions.AddButton()
                                    .SetId("ae_outfit_store")
                                    .SetText("Save Current Look")
                                    .SetTooltip("Saves your worn clothes, and your helmet if you wear one, to this outfit.")
                                    .BindOnClicked(model => model.OnClickStoreOutfit())
                                    .SetWidth(OutfitStoreButtonWidth)
                                    .SetHeight(32f);
                                actions.AddButton()
                                    .SetId("ae_outfit_wear")
                                    .SetText("Wear")
                                    .BindIsEnabled(model => model.IsOutfitWearEnabled)
                                    .BindOnClicked(model => model.OnClickWearOutfit())
                                    .SetWidth(OutfitButtonWidth)
                                    .SetHeight(32f);
                                actions.AddButton()
                                    .SetId("ae_outfit_undo")
                                    .SetText("Undo")
                                    .SetTooltip("Undo: restores the look you wore before the last outfit change.")
                                    .BindIsEnabled(model => model.IsUndoOutfitEnabled)
                                    .BindOnClicked(model => model.OnClickUndoOutfit())
                                    .SetWidth(OutfitButtonWidth)
                                    .SetHeight(32f);
                                actions.AddSpacer();
                            });
                        });
                    });
                });
            });
        }

        // ---------------------------------------------------------------- settings
        private void BuildSettings(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.Y);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsArmorDisplayVisible);
                    row.AddSpacer();
                    row.AddCheckBox()
                        .SetText("Show Helmet")
                        .SetHeight(26f)
                        .BindIsChecked(model => model.ShowHelmet);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsArmorDisplayVisible);
                    row.AddSpacer();
                    row.AddCheckBox()
                        .SetText("Show Cloak")
                        .SetHeight(26f)
                        .BindIsChecked(model => model.ShowCloak);
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetId("ae_height_increase")
                        .SetText("Increase Height")
                        .SetHeight(32f)
                        .SetWidth(128f)
                        .BindOnClicked(model => model.OnIncreaseAppearanceScale());
                    row.AddButton()
                        .SetId("ae_height_decrease")
                        .SetText("Decrease Height")
                        .SetHeight(32f)
                        .SetWidth(128f)
                        .BindOnClicked(model => model.OnDecreaseAppearanceScale());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetId("ae_head_increase")
                        .SetText("Increase Head")
                        .SetHeight(32f)
                        .SetWidth(128f)
                        .BindOnClicked(model => model.OnIncreaseHeadScale());
                    row.AddButton()
                        .SetId("ae_head_decrease")
                        .SetText("Decrease Head")
                        .SetHeight(32f)
                        .SetWidth(128f)
                        .BindOnClicked(model => model.OnDecreaseHeadScale());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetId("ae_settings_save")
                        .SetText("Save")
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickSaveSettings());
                    row.AddSpacer();
                    row.BindIsVisible(model => model.IsPlayerSettingsVisible);
                });
            });
        }
    }
}
