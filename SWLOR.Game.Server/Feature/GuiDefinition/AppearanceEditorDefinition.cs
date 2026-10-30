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
    /// Appearance Editor: body map | colours (swatches, selected part, palette, RGB picker). The
    /// selected part's picture opens the part browser window. The selected slot, swatch and
    /// palette cell are shown with NUI highlights.
    /// </summary>
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        public const float MapWidth = 280f;
        /// <summary>The colours panel's width in the default window; it grows with the window.</summary>
        public const float ColoursWidth = 376f;
        public const float WeaponCategoryWidth = 200f;
        /// <summary>
        /// The editor's default size. The four tabs need about 650 of the width (NUI keeps about
        /// 150 per tab). The window can be resized: the colours panel takes the extra width and
        /// both panels the extra height, and a smaller window scrolls its content.
        /// </summary>
        public const float EditorWidth = 710f;
        public const float EditorHeight = 680f;
        public const float TileSize = 96f; // pictures are 96x96; drawn 1:1 so they stay sharp
        public const float PreviewColumnWidth = 150f;
        /// <summary>Holds three item swatches and their row number, or two body swatches.</summary>
        private const float SwatchesColumnWidth = 196f;
        private const float PreviewArrowWidth = 30f;
        private const float PreviewViewButtonWidth = 56f;
        private const float SlotSize = 52f;
        /// <summary>A swatch is about as wide as its Leather / Cloth / Metal heading.</summary>
        private const float SwatchWidth = 52f;
        private const float SwatchHeight = 24f;
        private const float SwatchRowLabelWidth = 20f;
        /// <summary>A body colour's name ("Tattoo 1") is wider than its swatch.</summary>
        private const float BodySwatchColumnWidth = 72f;
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

        private const float SideColumnWidth = 58f;
        private const float PairColumnWidth = 58f;
        private const float CenterColumnWidth = 120f;
        private const float NeckSize = 40f;
        private const float ChestSize = 78f;
        private const float BeltSize = 44f;
        private const float PelvisSize = 60f;

        internal static readonly GuiColor Highlight = new(255, 196, 40);

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
                case AppearanceEditorViewModel.SettingsPartial: definition.BuildSettings(panel); break;
                case AppearanceEditorViewModel.OutfitsPartial: definition.BuildOutfits(panel); break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(partialName), partialName, "Unknown appearance editor panel.");
            }
            return panel;
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AppearanceEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, EditorWidth, EditorHeight)
                .SetTitle("Appearance Editor")
                .BindOnClosed(model => model.OnCloseWindow())
                .DefinePartialView(AppearanceEditorViewModel.EditorMainPartial, Flush(BuildAppearanceEditor))
                .DefinePartialView(AppearanceEditorViewModel.EditorArmorPartial, Flush(BuildEquipmentEditor))
                .DefinePartialView(AppearanceEditorViewModel.EditorWeaponPartial, Flush(BuildWeaponEditor))
                .DefinePartialView(AppearanceEditorViewModel.SettingsPartial, Flush(BuildSettings))
                .DefinePartialView(AppearanceEditorViewModel.OutfitsPartial, Flush(BuildOutfits))
                .AddStandardLayout(layout =>
                {
                    layout.SetTabPanelHeight(TabPanelHeight);
                    layout.SetCompactContent();
                    layout.AddTabRow(BuildNavigation);
                    layout.SetContentPartialElement(AppearanceEditorViewModel.MainPartialElement);
                });

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
        private void BuildAppearanceEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);
                    row.AddGroup(map => BuildBodyMap(map, equipment: false)).SetWidth(MapWidth);
                    row.AddGroup(colours => BuildColours(colours, BuildBodySwatches));
                });
            });
        }

        private void BuildEquipmentEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                AddEquipmentTabs(col);
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);
                    row.AddColumn(left =>
                    {
                        left.AddRow(mapRow => mapRow.AddGroup(map => BuildBodyMap(map, equipment: true)));
                        AddQuickOutfitRows(left);
                    }).SetWidth(MapWidth);
                    row.AddGroup(colours => BuildColours(colours, BuildItemSwatches));
                });
            });
        }

        private void BuildWeaponEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.None);
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
                    row.AddGroup(group =>
                    {
                        group.SetShowBorder(true).SetScrollbars(NuiScrollbars.None);
                        group.AddColumn(preview =>
                        {
                            preview.AddRow(previewRow =>
                            {
                                previewRow.AddColumn(AddPartPreview).SetWidth(PreviewColumnWidth);
                                previewRow.AddSpacer();
                            });
                            preview.AddRow(fill => fill.AddSpacer());
                        });
                    });
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
                AddSlot(center, AppearanceEditorSlot.Neck, true, NeckSize);
                AddSlot(center, AppearanceEditorSlot.Chest, true, ChestSize);
                AddSlot(center, AppearanceEditorSlot.Belt, true, BeltSize);
                AddSlot(center, AppearanceEditorSlot.Pelvis, true, PelvisSize);
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
                AddSlot(center, AppearanceEditorSlot.Chest, false, ChestSize);
                AddSlot(center, AppearanceEditorSlot.Pelvis, false, PelvisSize);
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
        /// <summary>
        /// The swatches beside the selected part's picture, then the palette and RGB picker.
        /// </summary>
        private void BuildColours(GuiGroup<AppearanceEditorViewModel> group,
            System.Action<GuiColumn<AppearanceEditorViewModel>> buildSwatches)
        {
            group.SetShowBorder(true).SetScrollbars(NuiScrollbars.Y);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    // Fixed widths keep the picture beside the swatches in a wider window.
                    row.AddColumn(swatches =>
                    {
                        buildSwatches(swatches);
                        swatches.AddRow(fill => fill.AddSpacer());
                    }).SetWidth(SwatchesColumnWidth);
                    row.AddColumn(AddPartPreview).SetWidth(PreviewColumnWidth);
                    row.AddSpacer();
                });
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

        /// <summary>
        /// The selected part's picture, which opens the part browser, with previous and next part
        /// buttons and a button that turns the picture between front, back and angle views.
        /// </summary>
        private static void AddPartPreview(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButtonImage()
                    .SetId("ae_part_preview")
                    .BindImageResref(model => model.PartPreviewImage)
                    .SetTooltip("Browse the parts for this slot.")
                    .SetWidth(TileSize)
                    .SetHeight(TileSize)
                    .SetMargin(2f)
                    .BindIsEnabled(model => model.IsPartPreviewEnabled)
                    .BindOnClicked(model => model.OnClickPartPreview());
                row.AddSpacer();
            });
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.PartPreviewLabel)
                    .SetHeight(18f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetId("ae_part_previous")
                    .SetText("<")
                    .SetTooltip("Previous part")
                    .BindIsEnabled(model => model.IsPartPreviewEnabled)
                    .SetWidth(PreviewArrowWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickPreviousPart());
                row.AddButton()
                    .SetId("ae_part_view")
                    .BindText(model => model.PreviewViewText)
                    .SetTooltip("Turns the pictures: front, back or angle.")
                    .BindIsEnabled(model => model.IsPreviewViewChoiceVisible)
                    .SetWidth(PreviewViewButtonWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickPreviewView());
                row.AddButton()
                    .SetId("ae_part_next")
                    .SetText(">")
                    .SetTooltip("Next part")
                    .BindIsEnabled(model => model.IsPartPreviewEnabled)
                    .SetWidth(PreviewArrowWidth)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnClickNextPart());
                row.AddSpacer();
            });
        }

        /// <summary>
        /// Two sections: the item's own colours, then the selected armor part's colours, which
        /// override the item colour on that part only.
        /// </summary>
        private void BuildItemSwatches(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer().SetWidth(SwatchRowLabelWidth);
                foreach (var label in new[] { "Leather", "Cloth", "Metal" })
                {
                    row.AddLabel().SetText(label).SetWidth(SwatchWidth + 4f).SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                }
                row.AddSpacer();
            });
            AddItemSwatchRow(col, null, "Global", AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1,
                AppearanceArmorColor.Metal1, partRow: false);
            AddItemSwatchRow(col, null, "Global", AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2,
                AppearanceArmorColor.Metal2, partRow: false);
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsPartColorsVisible);
                row.AddLabel()
                    .BindText(model => model.SelectedPartColorsTitle)
                    .SetTooltip("Colours for this part only. An empty swatch uses the outfit colour; right-click a swatch to empty it.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(SwatchHeight);
            });
            AddItemSwatchRow(col, "1", "SelectedPart", AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1,
                AppearanceArmorColor.Metal1, partRow: true);
            AddItemSwatchRow(col, "2", "SelectedPart", AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2,
                AppearanceArmorColor.Metal2, partRow: true);
        }

        /// <param name="label">"1" or "2" before a part row; item rows leave the space empty.</param>
        private void AddItemSwatchRow(GuiColumn<AppearanceEditorViewModel> col, string label, string prefix,
            AppearanceArmorColor leather, AppearanceArmorColor cloth, AppearanceArmorColor metal, bool partRow)
        {
            col.AddRow(row =>
            {
                if (partRow)
                    row.BindIsVisible(model => model.IsPartColorsVisible);
                if (label == null)
                    row.AddSpacer().SetWidth(SwatchRowLabelWidth);
                else
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
                            .SetTooltip("Colours the whole item. Parts with an empty swatch use this colour.")
                            .BindOnClicked(model => model.OnClickBaseSwatch(copy));
                    }
                }
                row.AddSpacer();
            });
        }

        /// <summary>Skin and hair, then the two tattoo colours, each under its name.</summary>
        private void BuildBodySwatches(GuiColumn<AppearanceEditorViewModel> col)
        {
            var swatches = new[]
            {
                ("Skin", "Skin", "gui_pal_skin"), ("Hair", "Hair", "gui_pal_hair01"),
                ("Tattoo 1", "Tattoo1", "gui_pal_tattoo"), ("Tattoo 2", "Tattoo2", "gui_pal_tattoo")
            };
            for (var first = 0; first < swatches.Length; first += 2)
            {
                var pair = new[] { first, first + 1 };
                col.AddRow(row =>
                {
                    foreach (var index in pair)
                    {
                        row.AddLabel().SetText(swatches[index].Item1).SetWidth(BodySwatchColumnWidth).SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                    }
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    foreach (var index in pair)
                    {
                        var (_, name, texture) = swatches[index];
                        var copy = index;
                        // Centre the swatch (and its 2 px margins) under its name.
                        var gap = (BodySwatchColumnWidth - SwatchWidth - 4f) / 2f;
                        row.AddSpacer().SetWidth(gap);
                        AddSwatch(row, texture, "Body" + name)
                            .SetId($"ae_body_swatch_{name}")
                            .BindOnClicked(model => model.OnClickBodySwatch(copy));
                        row.AddSpacer().SetWidth(gap);
                    }
                    row.AddSpacer();
                });
            }
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
                row.AddLabel().SetText("Custom Colour (RGB)").SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
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
        /// last change. It sits outside the map's scrolled panel, where NUI combo boxes do not open.
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
