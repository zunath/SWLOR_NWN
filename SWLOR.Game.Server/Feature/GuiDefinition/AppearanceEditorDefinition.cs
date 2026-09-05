using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using System.Linq.Expressions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        private const float MainColorChannelRowHeight = 99f;
        private const float PartColorChannelButtonSize = 20f;
        private const float PaletteButtonSize = 18f;
        private const float PaletteWidth = 308f;
        private const float PaletteHeight = 246f;
        private const float PaletteGridHeight = 218f;
        private const float CategoryWidth = 200f;
        private const float PartCategoryListHeight = 450f;
        private const float PartListHeight = 280f;
        private const float ArmorPartDropdownWidth = 96f;
        private const float ArmorPartEditorHeight = 112f;
        private const float ArmorPartsHeight = 840f;

        // All expanding spans are resolved by the client. This tree is shared by every
        // window size; resizing must not require new layouts or binding replay.
        public static GuiGroup<AppearanceEditorViewModel> BuildEditorPanel(string partialName)
        {
            var definition = new AppearanceEditorDefinition();
            var panel = new GuiGroup<AppearanceEditorViewModel>().SetShowBorder(false);
            switch (partialName)
            {
                case AppearanceEditorViewModel.EditorMainPartial: definition.BuildMainEditor(panel); break;
                case AppearanceEditorViewModel.EditorArmorPartial: definition.BuildArmorEditor(panel); break;
                case AppearanceEditorViewModel.SettingsPartial: definition.BuildSettings(panel); break;
                default: throw new ArgumentOutOfRangeException(nameof(partialName), partialName, "Unknown appearance editor panel.");
            }
            return panel;
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AppearanceEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 590f, 740f)
                .SetTitle("Appearance Editor")
                .BindOnClosed(model => model.OnCloseWindow())

                .DefinePartialView(AppearanceEditorViewModel.EditorMainPartial, BuildMainEditor)

                .DefinePartialView(AppearanceEditorViewModel.EditorArmorPartial, BuildArmorEditor)

                .DefinePartialView(AppearanceEditorViewModel.SettingsPartial, BuildSettings)

                .DefinePartialView(AppearanceEditorViewModel.ArmorColorsClothLeather, partial =>
                {
                    BuildColorPalette(partial, "gui_pal_tattoo");
                })

                .DefinePartialView(AppearanceEditorViewModel.ArmorColorsMetal, partial =>
                {
                    BuildColorPalette(partial, "gui_pal_armor01");
                })

                .AddStandardLayout(layout =>
                {
                    layout.SetTabPanelHeight(84f);
                    layout.AddTabRow(BuildNavigation);
                    layout.AddTabRow(BuildEquipmentSelection);
                    layout.SetContentPartialElement(AppearanceEditorViewModel.MainPartialElement);
                });

            return _builder.Build();
        }

        private void BuildNavigation(GuiRow<AppearanceEditorViewModel> row)
        {
            row.SetHeight(28f);
            row.AddToggles()
                .AddOption("Appearance")
                .AddOption("Equipment")
                .BindSelectedValue(model => model.EditorTabToggleValue)
                .SetHeight(28f);
            row.AddToggles()
                .AddOption("Settings")
                .BindSelectedValue(model => model.SettingsTabToggleValue)
                .BindIsVisible(model => model.IsSettingsVisible)
                .SetWidth(150f)
                .SetHeight(28f);
        }

        private void BuildEquipmentSelection(GuiRow<AppearanceEditorViewModel> row)
        {
            row.BindIsVisible(model => model.IsEquipmentSelected);
            row.AddComboBox()
                .AddOption("Armor", 0)
                .AddOption("Helmet", 1)
                .AddOption("Cloak", 2)
                .AddOption("Weapon (Main)", 3)
                .AddOption("Weapon (Off)", 4)
                .BindSelectedIndex(model => model.SelectedItemTypeIndex)
                .SetWidth(240f)
                .SetHeight(32f);
            row.AddButton()
                .SetText("Outfits")
                .SetWidth(140f)
                .SetHeight(32f)
                .BindOnClicked(model => model.OnClickOutfits());
        }

        private void BuildMainEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            // Each side owns its vertical scrolling while the client gives the detail
            // group the width remaining beside the category rail.
            partial.SetScrollbars(NuiScrollbars.None);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("No item is equipped or the equipped item cannot be modified.")
                        .BindIsVisible(model => model.DoesNotHaveItemEquipped);

                    row.SetHeight(20f);
                    row.BindIsVisible(model => model.DoesNotHaveItemEquipped);
                });

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasItemEquipped);

                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.SetHeight(162f);
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .SetId("ae_color_category")
                                            .BindText(model => model.ColorCategoryOptions)
                                            .BindIsToggled(model => model.ColorCategorySelected)
                                            .BindOnClicked(model => model.OnSelectColorCategory());
                                    });
                                })
                                    .BindRowCount(model => model.ColorCategoryOptions)
                                    .SetHeight(154f);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.SetHeight(PartCategoryListHeight + 8f);
                                row2.AddList(template =>
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
                                    .SetHeight(PartCategoryListHeight);
                            });

                            col2.AddRow(row2 => row2.AddSpacer());

                        });
                    }).SetWidth(CategoryWidth);

                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddGroup(palette => BuildColorPalette(palette, showTarget: false))
                                    .SetId("ae_color_palette")
                                    .BindIsVisible(model => model.IsColorPickerVisible);

                                // A fixed palette alone pulls the group's private layout back
                                // to its width. Give this row a place to absorb extra space.
                                row2.AddSpacer();
                            });

                            BuildCustomTintEditor(col2);

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .SetId("ae_part_select")
                                            .BindText(model => model.PartOptions)
                                            .BindIsToggled(model => model.PartSelected)
                                            .BindOnClicked(model => model.OnSelectPart());
                                    });
                                })
                                    .BindRowCount(model => model.PartOptions)
                                    .SetHeight(PartListHeight);
                            });

                            col2.AddRow(row2 =>
                            {
                                // Dimensions on the buttons would also disable their
                                // native equal-width sharing. Size their shared row instead.
                                row2.SetHeight(40f);
                                row2.AddButton()
                                    .SetId("ae_previous_part")
                                    .SetText("Previous Part")
                                    .BindOnClicked(model => model.OnPreviousPart());

                                row2.AddButton()
                                    .SetId("ae_next_part")
                                    .SetText("Next Part")
                                    .BindOnClicked(model => model.OnNextPart());
                            });

                        });
                    });
                });
            });
        }

        private void BuildCustomTintEditor(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsCustomTintAvailable);

                row.AddColorPicker()
                    .BindSelectedColor(model => model.SelectedTintColor)
                    .BindIsEnabled(model => model.IsCustomTintAvailable)
                    .SetHeight(128f);
            });

            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsCustomTintAvailable);
                row.AddSpacer();

                row.AddLabel()
                    .SetText("R")
                    .SetWidth(18f)
                    .SetHeight(32f)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddTextEdit()
                    .BindValue(model => model.CustomTintRed)
                    .BindIsEnabled(model => model.IsCustomTintAvailable)
                    .SetMaxLength(3)
                    .SetWidth(48f)
                    .SetHeight(32f);

                row.AddLabel()
                    .SetText("G")
                    .SetWidth(18f)
                    .SetHeight(32f)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddTextEdit()
                    .BindValue(model => model.CustomTintGreen)
                    .BindIsEnabled(model => model.IsCustomTintAvailable)
                    .SetMaxLength(3)
                    .SetWidth(48f)
                    .SetHeight(32f);

                row.AddLabel()
                    .SetText("B")
                    .SetWidth(18f)
                    .SetHeight(32f)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                row.AddTextEdit()
                    .BindValue(model => model.CustomTintBlue)
                    .BindIsEnabled(model => model.IsCustomTintAvailable)
                    .SetMaxLength(3)
                    .SetWidth(48f)
                    .SetHeight(32f);

                row.AddSpacer();
            });
        }

        private void BuildArmorEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.Auto);
            void BuildMainColorChannels(GuiColumn<AppearanceEditorViewModel> col)
            {
                col.AddRow(row =>
                {
                    row.SetHeight(28f).SetMargin(0f);
                    foreach (var label in new[] { "Leather", "Cloth", "Metal" })
                    {
                        row.AddLabel()
                            .SetText(label)
                            .SetMargin(0f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    }
                });

                col.AddRow(row =>
                {
                    row.SetHeight(MainColorChannelRowHeight).SetMargin(0f);
                    CreateGlobalColorSwatch(row, "gui_pal_tattoo", model => model.GlobalLeather1Region, AppearanceArmorColor.Leather1);
                    CreateGlobalColorSwatch(row, "gui_pal_tattoo", model => model.GlobalCloth1Region, AppearanceArmorColor.Cloth1);
                    CreateGlobalColorSwatch(row, "gui_pal_armor01", model => model.GlobalMetal1Region, AppearanceArmorColor.Metal1);
                });
                col.AddRow(row =>
                {
                    row.SetHeight(MainColorChannelRowHeight).SetMargin(0f);
                    CreateGlobalColorSwatch(row, "gui_pal_tattoo", model => model.GlobalLeather2Region, AppearanceArmorColor.Leather2);
                    CreateGlobalColorSwatch(row, "gui_pal_tattoo", model => model.GlobalCloth2Region, AppearanceArmorColor.Cloth2);
                    CreateGlobalColorSwatch(row, "gui_pal_armor01", model => model.GlobalMetal2Region, AppearanceArmorColor.Metal2);
                });
            }

            void CreatePartEditor(
                GuiColumn<AppearanceEditorViewModel> stack,
                string partName,
                AppearanceArmor partType,
                AppearanceEditorViewModel.ColorTarget colorTarget,
                Expression<Func<AppearanceEditorViewModel, GuiBindingList<GuiComboEntry>>> optionsBinding,
                Expression<Func<AppearanceEditorViewModel, int>> selectionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> leather1RegionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> leather2RegionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> cloth1RegionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> cloth2RegionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> metal1RegionBinding,
                Expression<Func<AppearanceEditorViewModel, GuiRectangle>> metal2RegionBinding)
            {
                stack.AddRow(partRow => partRow.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText(partName)
                            .SetHeight(PartColorChannelButtonSize)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetId("ae_previous_" + partType)
                            .SetText("<")
                            .SetHeight(24f)
                            .SetWidth(24f)
                            .SetMargin(0f)
                            .BindOnClicked(model => model.OnClickAdjustArmorPart(partType, -1));

                        row.AddComboBox()
                            .SetWidth(ArmorPartDropdownWidth)
                            .SetHeight(24f)
                            .SetMargin(0f)
                            .BindOptions(optionsBinding)
                            .BindSelectedIndex(selectionBinding);

                        row.AddButton()
                            .SetId("ae_next_" + partType)
                            .SetText(">")
                            .SetHeight(24f)
                            .SetWidth(24f)
                            .SetMargin(0f)
                            .BindOnClicked(model => model.OnClickAdjustArmorPart(partType, 1));
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        CreateFilledButton(
                            row,
                            "gui_pal_tattoo",
                            leather1RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Leather1),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Leather1));
                        CreateFilledButton(
                            row,
                            "gui_pal_tattoo",
                            cloth1RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Cloth1),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Cloth1));
                        CreateFilledButton(
                            row,
                            "gui_pal_armor01",
                            metal1RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Metal1),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Metal1));
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        CreateFilledButton(
                            row,
                            "gui_pal_tattoo",
                            leather2RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Leather2),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Leather2));
                        CreateFilledButton(
                            row,
                            "gui_pal_tattoo",
                            cloth2RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Cloth2),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Cloth2));
                        CreateFilledButton(
                            row,
                            "gui_pal_armor01",
                            metal2RegionBinding,
                            PartColorChannelButtonSize,
                            2f,
                            model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Metal2),
                            model => model.OnClickClearColor(colorTarget, AppearanceArmorColor.Metal2));
                        row.AddSpacer();
                    });
                }).SetHeight(ArmorPartEditorHeight));
            }

            void AddPartColumn(GuiRow<AppearanceEditorViewModel> row, Action<GuiColumn<AppearanceEditorViewModel>> build)
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
                    group.AddColumn(build);
                });
            }

            void CreateGap(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                // Gutters must stay narrow while the three editor columns expand.
                mainRow.AddSpacer().SetWidth(6f);
            }

            void BuildParts(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                // Any explicit dimension on a child disables native equal spacing in
                // both axes. Bound the row's height so all three groups share its width.
                mainRow.SetHeight(ArmorPartsHeight);
                AddPartColumn(mainRow, col =>
                {
                    CreatePartEditor(
                        col,
                        "Left Shoulder",
                        AppearanceArmor.LeftShoulder,
                        AppearanceEditorViewModel.ColorTarget.LeftShoulder,
                        model => model.LeftShoulderOptions,
                        model => model.LeftShoulderSelection,
                        model => model.LeftShoulderLeather1Region,
                        model => model.LeftShoulderLeather2Region,
                        model => model.LeftShoulderCloth1Region,
                        model => model.LeftShoulderCloth2Region,
                        model => model.LeftShoulderMetal1Region,
                        model => model.LeftShoulderMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Bicep",
                        AppearanceArmor.LeftBicep,
                        AppearanceEditorViewModel.ColorTarget.LeftBicep,
                        model => model.LeftBicepOptions,
                        model => model.LeftBicepSelection,
                        model => model.LeftBicepLeather1Region,
                        model => model.LeftBicepLeather2Region,
                        model => model.LeftBicepCloth1Region,
                        model => model.LeftBicepCloth2Region,
                        model => model.LeftBicepMetal1Region,
                        model => model.LeftBicepMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Forearm",
                        AppearanceArmor.LeftForearm,
                        AppearanceEditorViewModel.ColorTarget.LeftForearm,
                        model => model.LeftForearmOptions,
                        model => model.LeftForearmSelection,
                        model => model.LeftForearmLeather1Region,
                        model => model.LeftForearmLeather2Region,
                        model => model.LeftForearmCloth1Region,
                        model => model.LeftForearmCloth2Region,
                        model => model.LeftForearmMetal1Region,
                        model => model.LeftForearmMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Hand",
                        AppearanceArmor.LeftHand,
                        AppearanceEditorViewModel.ColorTarget.LeftHand,
                        model => model.LeftHandOptions,
                        model => model.LeftHandSelection,
                        model => model.LeftHandLeather1Region,
                        model => model.LeftHandLeather2Region,
                        model => model.LeftHandCloth1Region,
                        model => model.LeftHandCloth2Region,
                        model => model.LeftHandMetal1Region,
                        model => model.LeftHandMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Thigh",
                        AppearanceArmor.LeftThigh,
                        AppearanceEditorViewModel.ColorTarget.LeftThigh,
                        model => model.LeftThighOptions,
                        model => model.LeftThighSelection,
                        model => model.LeftThighLeather1Region,
                        model => model.LeftThighLeather2Region,
                        model => model.LeftThighCloth1Region,
                        model => model.LeftThighCloth2Region,
                        model => model.LeftThighMetal1Region,
                        model => model.LeftThighMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Shin",
                        AppearanceArmor.LeftShin,
                        AppearanceEditorViewModel.ColorTarget.LeftShin,
                        model => model.LeftShinOptions,
                        model => model.LeftShinSelection,
                        model => model.LeftShinLeather1Region,
                        model => model.LeftShinLeather2Region,
                        model => model.LeftShinCloth1Region,
                        model => model.LeftShinCloth2Region,
                        model => model.LeftShinMetal1Region,
                        model => model.LeftShinMetal2Region);
                    CreatePartEditor(
                        col,
                        "Left Foot",
                        AppearanceArmor.LeftFoot,
                        AppearanceEditorViewModel.ColorTarget.LeftFoot,
                        model => model.LeftFootOptions,
                        model => model.LeftFootSelection,
                        model => model.LeftFootLeather1Region,
                        model => model.LeftFootLeather2Region,
                        model => model.LeftFootCloth1Region,
                        model => model.LeftFootCloth2Region,
                        model => model.LeftFootMetal1Region,
                        model => model.LeftFootMetal2Region);
                });

                CreateGap(mainRow);

                AddPartColumn(mainRow, col =>
                {
                    CreatePartEditor(
                        col,
                        "Neck",
                        AppearanceArmor.Neck,
                        AppearanceEditorViewModel.ColorTarget.Neck,
                        model => model.NeckOptions,
                        model => model.NeckSelection,
                        model => model.NeckLeather1Region,
                        model => model.NeckLeather2Region,
                        model => model.NeckCloth1Region,
                        model => model.NeckCloth2Region,
                        model => model.NeckMetal1Region,
                        model => model.NeckMetal2Region);
                    CreatePartEditor(
                        col,
                        "Chest",
                        AppearanceArmor.Torso,
                        AppearanceEditorViewModel.ColorTarget.Chest,
                        model => model.ChestOptions,
                        model => model.ChestSelection,
                        model => model.ChestLeather1Region,
                        model => model.ChestLeather2Region,
                        model => model.ChestCloth1Region,
                        model => model.ChestCloth2Region,
                        model => model.ChestMetal1Region,
                        model => model.ChestMetal2Region);
                    CreatePartEditor(
                        col,
                        "Belt",
                        AppearanceArmor.Belt,
                        AppearanceEditorViewModel.ColorTarget.Belt,
                        model => model.BeltOptions,
                        model => model.BeltSelection,
                        model => model.BeltLeather1Region,
                        model => model.BeltLeather2Region,
                        model => model.BeltCloth1Region,
                        model => model.BeltCloth2Region,
                        model => model.BeltMetal1Region,
                        model => model.BeltMetal2Region);
                    CreatePartEditor(
                        col,
                        "Pelvis",
                        AppearanceArmor.Pelvis,
                        AppearanceEditorViewModel.ColorTarget.Pelvis,
                        model => model.PelvisOptions,
                        model => model.PelvisSelection,
                        model => model.PelvisLeather1Region,
                        model => model.PelvisLeather2Region,
                        model => model.PelvisCloth1Region,
                        model => model.PelvisCloth2Region,
                        model => model.PelvisMetal1Region,
                        model => model.PelvisMetal2Region);
                    CreatePartEditor(
                        col,
                        "Robe",
                        AppearanceArmor.Robe,
                        AppearanceEditorViewModel.ColorTarget.Robe,
                        model => model.RobeOptions,
                        model => model.RobeSelection,
                        model => model.RobeLeather1Region,
                        model => model.RobeLeather2Region,
                        model => model.RobeCloth1Region,
                        model => model.RobeCloth2Region,
                        model => model.RobeMetal1Region,
                        model => model.RobeMetal2Region);

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                });

                CreateGap(mainRow);

                AddPartColumn(mainRow, col =>
                {
                    CreatePartEditor(
                        col,
                        "Right Shoulder",
                        AppearanceArmor.RightShoulder,
                        AppearanceEditorViewModel.ColorTarget.RightShoulder,
                        model => model.RightShoulderOptions,
                        model => model.RightShoulderSelection,
                        model => model.RightShoulderLeather1Region,
                        model => model.RightShoulderLeather2Region,
                        model => model.RightShoulderCloth1Region,
                        model => model.RightShoulderCloth2Region,
                        model => model.RightShoulderMetal1Region,
                        model => model.RightShoulderMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Bicep",
                        AppearanceArmor.RightBicep,
                        AppearanceEditorViewModel.ColorTarget.RightBicep,
                        model => model.RightBicepOptions,
                        model => model.RightBicepSelection,
                        model => model.RightBicepLeather1Region,
                        model => model.RightBicepLeather2Region,
                        model => model.RightBicepCloth1Region,
                        model => model.RightBicepCloth2Region,
                        model => model.RightBicepMetal1Region,
                        model => model.RightBicepMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Forearm",
                        AppearanceArmor.RightForearm,
                        AppearanceEditorViewModel.ColorTarget.RightForearm,
                        model => model.RightForearmOptions,
                        model => model.RightForearmSelection,
                        model => model.RightForearmLeather1Region,
                        model => model.RightForearmLeather2Region,
                        model => model.RightForearmCloth1Region,
                        model => model.RightForearmCloth2Region,
                        model => model.RightForearmMetal1Region,
                        model => model.RightForearmMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Hand",
                        AppearanceArmor.RightHand,
                        AppearanceEditorViewModel.ColorTarget.RightHand,
                        model => model.RightHandOptions,
                        model => model.RightHandSelection,
                        model => model.RightHandLeather1Region,
                        model => model.RightHandLeather2Region,
                        model => model.RightHandCloth1Region,
                        model => model.RightHandCloth2Region,
                        model => model.RightHandMetal1Region,
                        model => model.RightHandMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Thigh",
                        AppearanceArmor.RightThigh,
                        AppearanceEditorViewModel.ColorTarget.RightThigh,
                        model => model.RightThighOptions,
                        model => model.RightThighSelection,
                        model => model.RightThighLeather1Region,
                        model => model.RightThighLeather2Region,
                        model => model.RightThighCloth1Region,
                        model => model.RightThighCloth2Region,
                        model => model.RightThighMetal1Region,
                        model => model.RightThighMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Shin",
                        AppearanceArmor.RightShin,
                        AppearanceEditorViewModel.ColorTarget.RightShin,
                        model => model.RightShinOptions,
                        model => model.RightShinSelection,
                        model => model.RightShinLeather1Region,
                        model => model.RightShinLeather2Region,
                        model => model.RightShinCloth1Region,
                        model => model.RightShinCloth2Region,
                        model => model.RightShinMetal1Region,
                        model => model.RightShinMetal2Region);
                    CreatePartEditor(
                        col,
                        "Right Foot",
                        AppearanceArmor.RightFoot,
                        AppearanceEditorViewModel.ColorTarget.RightFoot,
                        model => model.RightFootOptions,
                        model => model.RightFootSelection,
                        model => model.RightFootLeather1Region,
                        model => model.RightFootLeather2Region,
                        model => model.RightFootCloth1Region,
                        model => model.RightFootCloth2Region,
                        model => model.RightFootMetal1Region,
                        model => model.RightFootMetal2Region);
                });
            }

            void BuildFooter(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                mainRow.AddColumn(mainCol =>
                {
                    mainCol.AddRow(row =>
                    {
                        row.AddButton()
                            .SetId("ae_copy_right")
                            .SetText("Copy to Right -->")
                            .BindIsEnabled(model => model.IsCopyEnabled)
                            .BindOnClicked(model => model.OnClickCopyToRight());
                    });
                });

                mainRow.AddColumn(mainCol =>
                {
                    mainCol.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                });

                mainRow.AddColumn(mainCol =>
                {
                    mainCol.AddRow(row =>
                    {
                        row.AddButton()
                            .SetId("ae_copy_left")
                            .SetText("<-- Copy to Left")
                            .BindIsEnabled(model => model.IsCopyEnabled)
                            .BindOnClicked(model => model.OnClickCopyToLeft());
                    });
                });
            }

            partial.AddColumn(mainCol =>
            {
                mainCol.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("No item is equipped or the equipped item cannot be modified.")
                        .BindIsVisible(model => model.DoesNotHaveItemEquipped);

                    row.SetHeight(20f);
                    row.BindIsVisible(model => model.DoesNotHaveItemEquipped);
                });

                mainCol.AddRow(mainRow =>
                {
                    mainRow.BindIsVisible(model => model.HasItemEquipped);
                    mainRow.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddPartialView(AppearanceEditorViewModel.ArmorColorElement)
                                .SetWidth(PaletteWidth)
                                .SetHeight(PaletteHeight);
                            row.AddGroup(channels =>
                            {
                                channels.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
                                channels.AddColumn(BuildMainColorChannels);
                            });
                        });

                        BuildCustomTintEditor(col);

                        col.AddRow(row =>
                        {
                            BuildParts(row);
                        });

                        col.AddRow(row =>
                        {
                            BuildFooter(row);
                        });
                    });
                });

            });
        }

        private void CreateGlobalColorSwatch(
            GuiRow<AppearanceEditorViewModel> row,
            string texture,
            Expression<Func<AppearanceEditorViewModel, GuiRectangle>> regionBinding,
            AppearanceArmorColor channel)
        {
            // Ordinary images stretch their cropped region with the widget. Draw-list
            // destinations are fixed rectangles and cannot track the native layout.
            row.AddImage()
                .SetId("ae_color_" + GuiHelper<AppearanceEditorViewModel>.GetPropertyName(regionBinding))
                .SetResref(texture)
                .BindRegion(regionBinding)
                .SetAspect(NuiAspect.Stretch)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetVerticalAlign(NuiVerticalAlign.Top)
                .SetMargin(2f)
                .BindOnMouseDown(model => model.OnMouseDownGlobalColor(channel));
        }

        private GuiButton<AppearanceEditorViewModel> CreateFilledButton(
            GuiExpandableComponent<AppearanceEditorViewModel> component,
            string texture,
            Expression<Func<AppearanceEditorViewModel, GuiRectangle>> regionBind,
            float buttonSize,
            float drawOffset,
            Expression<Func<AppearanceEditorViewModel, Action>> onClickBind,
            Expression<Func<AppearanceEditorViewModel, Action>> onClickClearColor,
            GuiRectangle staticRegion = null,
            Expression<Func<AppearanceEditorViewModel, string>> textureBinding = null)
        {
            var button = component.AddButton()
                .SetText("")
                .SetWidth(buttonSize)
                .SetHeight(buttonSize)
                .SetMargin(0f)
                .SetIsEncouraged(true)
                .AddDrawList(drawList =>
                {
                    drawList.AddImage(image =>
                    {
                        if (textureBinding != null)
                            image.BindResref(textureBinding);
                        else
                            image.SetResref(texture);
                        image.SetPosition(drawOffset, drawOffset, buttonSize - drawOffset * 2f, buttonSize - drawOffset * 2f);
                        image.SetAspect(NuiAspect.Stretch);
                        image.SetHorizontalAlign(NuiHorizontalAlign.Left);
                        image.SetVerticalAlign(NuiVerticalAlign.Top);
                        if (regionBind != null)
                        {
                            image.BindDrawTextureRegion(regionBind);
                        }

                        if (staticRegion != null)
                        {
                            image.SetDrawTextureRegion(staticRegion);
                        }
                    });
                })
                .BindOnClicked(onClickBind);
            if (onClickClearColor != null)
                button.BindOnMouseDown(onClickClearColor);
            if (regionBind != null)
                button.SetId("ae_color_" + GuiHelper<AppearanceEditorViewModel>.GetPropertyName(regionBind));
            return button;
        }

        private void BuildSettings(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.SetScrollbars(NuiScrollbars.Y);
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddCheckBox()
                        .SetText("Show Helmet")
                        .SetHeight(26f)
                        .BindIsChecked(model => model.ShowHelmet);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
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
                        .SetId("ae_settings_save")
                        .SetText("Save")
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickSaveSettings());
                    row.AddSpacer();
                });
            });
        }

        private void BuildColorPalette(GuiGroup<AppearanceEditorViewModel> group, string texture = null, bool showTarget = true)
        {
            group.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            group.SetWidth(PaletteWidth);
            group.SetHeight(showTarget ? PaletteHeight : PaletteGridHeight);
            group.AddColumn(col =>
            {
                if (showTarget)
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.ColorTargetText)
                            .BindIsVisible(model => model.IsEquipmentSelected);
                    });
                }

                const int UIColorsPerRow = 16;
                const int ColorTotalCount = TintMapMaterialRegistry.PaletteColorCount;
                const int RowCount = (ColorTotalCount + UIColorsPerRow - 1) / UIColorsPerRow;

                for (var y = 0; y < RowCount; ++y)
                {
                    var yCopy = y;
                    col.AddRow(uiRow =>
                    {
                        uiRow.SetHeight(PaletteButtonSize);
                        uiRow.SetMargin(0f);
                        for (var x = 0; x < UIColorsPerRow; ++x)
                        {
                            var paletteIndex = yCopy * UIColorsPerRow + x;
                            if (paletteIndex >= ColorTotalCount)
                                break;

                            var row = paletteIndex / AppearanceEditorViewModel.TextureColorsPerRow;
                            var offset = paletteIndex % AppearanceEditorViewModel.TextureColorsPerRow;

                            var region = new GuiRectangle(
                                offset * AppearanceEditorViewModel.ColorSize + 2,
                                row * AppearanceEditorViewModel.ColorSize + 2,
                                AppearanceEditorViewModel.ColorSize - 4,
                                AppearanceEditorViewModel.ColorSize - 4);

                            CreateFilledButton(
                                uiRow,
                                texture,
                                null,
                                PaletteButtonSize,
                                2f,
                                model => model.OnClickColorPalette(paletteIndex),
                                null,
                                region,
                                textureBinding: texture == null ? model => model.ColorSheetResref : null)
                                .SetId("ae_palette_" + paletteIndex);
                        }
                    });
                }

            });
        }
    }
}
