using NRediSearch.Aggregation;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using System.Linq.Expressions;
using System;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        private const float MainColorChannelButtonSize = 72f;
        private const float PartColorChannelButtonSize = 16f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AppearanceEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 476.57895f, 600f)
                .SetTitle("Appearance Editor")
                .BindOnClosed(model => model.OnCloseWindow())

                .DefinePartialView(AppearanceEditorViewModel.EditorHeaderPartial, BuildEditorHeader)

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

                .AddColumn(BuildNavigation);

            return _builder.Build();
        }

        private void BuildNavigation(GuiColumn<AppearanceEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddToggleButton()
                    .SetText("Appearance")
                    .SetHeight(32f)
                    .BindIsToggled(model => model.IsAppearanceSelected)
                    .BindOnClicked(model => model.OnSelectAppearance());

                row.AddToggleButton()
                    .SetText("Equipment")
                    .SetHeight(32f)
                    .BindIsToggled(model => model.IsEquipmentSelected)
                    .BindOnClicked(model => model.OnSelectEquipment());

                row.AddToggleButton()
                    .SetText("Settings")
                    .SetHeight(32f)
                    .BindIsToggled(model => model.IsSettingsSelected)
                    .BindOnClicked(model => model.OnSelectSettings())
                    .BindIsVisible(model => model.IsSettingsVisible);

                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.AddPartialView(AppearanceEditorViewModel.MainPartialElement);
            });
        }

        private void BuildEditorHeader(GuiGroup<AppearanceEditorViewModel> partial)
        {
            partial.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsEquipmentSelected);

                    row.AddSpacer();

                    row.AddComboBox()
                        .AddOption("Armor", 0)
                        .AddOption("Helmet", 1)
                        .AddOption("Cloak", 2)
                        .AddOption("Weapon (Main)", 3)
                        .AddOption("Weapon (Off)", 4)
                        .BindSelectedIndex(model => model.SelectedItemTypeIndex);

                    row.AddButton()
                        .SetText("Outfits")
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickOutfits());

                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddPartialView(AppearanceEditorViewModel.EditorPartialElement);
                });
            });
        }

        private void BuildMainEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
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

                    row.AddColumn(col2 =>
                    {
                        col2.AddRow(row2 =>
                        {
                            row2.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.ColorCategoryOptions)
                                        .BindIsToggled(model => model.ColorCategorySelected)
                                        .BindOnClicked(model => model.OnSelectColorCategory());
                                });
                            })
                                .BindRowCount(model => model.ColorCategoryOptions);
                        });

                        col2.AddRow(row2 =>
                        {
                            row2.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.PartCategoryOptions)
                                        .BindIsToggled(model => model.PartCategorySelected)
                                        .BindOnClicked(model => model.OnSelectPartCategory());
                                });
                            })
                                .BindRowCount(model => model.PartCategoryOptions);
                        });

                    });


                    row.AddColumn(col2 =>
                    {
                        col2.AddRow(row2 =>
                        {
                            row2.AddImage()
                                .BindResref(model => model.ColorSheetResref)
                                .SetHeight(176f)
                                .SetWidth(256f)
                                .SetVerticalAlign(NuiVerticalAlign.Top)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetAspect(NuiAspect.ExactScaled)
                                .BindOnMouseDown(model => model.OnSelectColor())
                                .BindIsVisible(model => model.IsColorPickerVisible);
                        });

                        col2.AddRow(row2 =>
                        {
                            row2.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.PartOptions)
                                        .BindIsToggled(model => model.PartSelected)
                                        .BindOnClicked(model => model.OnSelectPart());
                                });
                            })
                                .BindRowCount(model => model.PartOptions)
                                .SetWidth(256f);
                        });

                        col2.AddRow(row2 =>
                        {
                            row2.AddButton()
                                .SetText("Previous Part")
                                .SetHeight(32f)
                                .SetWidth(128f)
                                .BindOnClicked(model => model.OnPreviousPart());

                            row2.AddButton()
                                .SetText("Next Part")
                                .SetHeight(32f)
                                .SetWidth(128f)
                                .BindOnClicked(model => model.OnNextPart());
                        });

                    });
                });
            });
        }

        private void BuildArmorEditor(GuiGroup<AppearanceEditorViewModel> partial)
        {
            void BuildMainColorChannels(GuiColumn<AppearanceEditorViewModel> col)
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHeight(20f)
                        .SetWidth(MainColorChannelButtonSize)
                        .SetText("Leather")
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Top);

                    row.AddLabel()
                        .SetHeight(20f)
                        .SetWidth(MainColorChannelButtonSize)
                        .SetText("Cloth")
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Top);

                    row.AddLabel()
                        .SetHeight(20f)
                        .SetWidth(MainColorChannelButtonSize)
                        .SetText("Metal")
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Top);
                });

                col.AddRow(row =>
                {
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo", 
                        model => model.GlobalLeather1Region, 
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather1));
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.GlobalCloth1Region,
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth1));
                    CreateFilledButton(
                        row, 
                        "gui_pal_armor01",
                        model => model.GlobalMetal1Region,
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Metal1));
                });
                col.AddRow(row =>
                {
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.GlobalLeather2Region,
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather2));
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.GlobalCloth2Region,
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth2));
                    CreateFilledButton(
                        row, 
                        "gui_pal_armor01",
                        model => model.GlobalMetal2Region,
                        MainColorChannelButtonSize, 
                        4f,
                        model => model.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Metal2));
                });
                col.AddRow(row =>
                {
                    row.AddSpacer()
                        .SetHeight(32f);
                });
            }

            void CreatePartEditor(
                GuiColumn<AppearanceEditorViewModel> col, 
                string partName,
                AppearanceArmor partType,
                AppearanceEditorViewModel.ColorTarget colorTarget,
                Expression<Func<AppearanceEditorViewModel, GuiBindingList<GuiComboEntry>>> optionsBinding,
                Expression<Func<AppearanceEditorViewModel, int>> selectionBinding)
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
                        .SetText("<")
                        .SetHeight(24f)
                        .SetWidth(24f)
                        .SetMargin(0f)
                        .BindOnClicked(model => model.OnClickAdjustArmorPart(partType, -1));

                    row.AddComboBox()
                        .SetHeight(24f)
                        .SetWidth(100f)
                        .SetMargin(0f)
                        .BindOptions(optionsBinding)
                        .BindSelectedIndex(selectionBinding);

                    row.AddButton()
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
                        model => model.DummyRegion, 
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Leather1));
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.DummyRegion,
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Cloth1));
                    CreateFilledButton(
                        row,
                        "gui_pal_armor01",
                        model => model.DummyRegion,
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Metal1));
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.DummyRegion,
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Leather2));
                    CreateFilledButton(
                        row,
                        "gui_pal_tattoo",
                        model => model.DummyRegion,
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Cloth2));
                    CreateFilledButton(
                        row, 
                        "gui_pal_armor01",
                        model => model.DummyRegion,
                        PartColorChannelButtonSize, 
                        2f,
                        model => model.OnClickColorTarget(colorTarget, AppearanceArmorColor.Metal2));
                    row.AddSpacer();
                });
            }

            void CreateGap(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                mainRow.AddColumn(col =>
                {
                    for (var x = 1; x <= 7; x++)
                    {
                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(6f)
                                .SetHeight(6f);
                        });
                    }
                });
            }

            void BuildParts(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                mainRow.AddColumn(col =>
                {
                    CreatePartEditor(
                        col, 
                        "Left Shoulder", 
                        AppearanceArmor.LeftShoulder,
                        AppearanceEditorViewModel.ColorTarget.LeftShoulder,
                        model => model.LeftShoulderOptions, 
                        model => model.LeftShoulderSelection);
                    CreatePartEditor(
                        col, 
                        "Left Bicep",
                        AppearanceArmor.LeftBicep,
                        AppearanceEditorViewModel.ColorTarget.LeftBicep,
                        model => model.LeftBicepOptions,
                        model => model.LeftBicepSelection);
                    CreatePartEditor(
                        col, 
                        "Left Forearm",
                        AppearanceArmor.LeftForearm,
                        AppearanceEditorViewModel.ColorTarget.LeftForearm,
                        model => model.LeftForearmOptions,
                        model => model.LeftForearmSelection);
                    CreatePartEditor(
                        col, 
                        "Left Hand",
                        AppearanceArmor.LeftHand,
                        AppearanceEditorViewModel.ColorTarget.LeftHand,
                        model => model.LeftHandOptions,
                        model => model.LeftHandSelection);
                    CreatePartEditor(
                        col, 
                        "Left Thigh",
                        AppearanceArmor.LeftThigh,
                        AppearanceEditorViewModel.ColorTarget.LeftThigh,
                        model => model.LeftThighOptions,
                        model => model.LeftThighSelection);
                    CreatePartEditor(
                        col, 
                        "Left Shin",
                        AppearanceArmor.LeftShin,
                        AppearanceEditorViewModel.ColorTarget.LeftShin,
                        model => model.LeftShinOptions,
                        model => model.LeftShinSelection);
                    CreatePartEditor(
                        col, 
                        "Left Foot",
                        AppearanceArmor.LeftFoot,
                        AppearanceEditorViewModel.ColorTarget.LeftFoot,
                        model => model.LeftFootOptions,
                        model => model.LeftFootSelection);
                });

                CreateGap(mainRow);

                mainRow.AddColumn(col =>
                {
                    CreatePartEditor(
                        col, 
                        "Neck",
                        AppearanceArmor.Neck,
                        AppearanceEditorViewModel.ColorTarget.Neck,
                        model => model.NeckOptions,
                        model => model.NeckSelection);
                    CreatePartEditor(
                        col, 
                        "Chest",
                        AppearanceArmor.Torso,
                        AppearanceEditorViewModel.ColorTarget.Chest,
                        model => model.ChestOptions,
                        model => model.ChestSelection);
                    CreatePartEditor(
                        col, 
                        "Belt",
                        AppearanceArmor.Belt,
                        AppearanceEditorViewModel.ColorTarget.Belt,
                        model => model.BeltOptions,
                        model => model.BeltSelection);
                    CreatePartEditor(
                        col, 
                        "Pelvis",
                        AppearanceArmor.Pelvis,
                        AppearanceEditorViewModel.ColorTarget.Pelvis,
                        model => model.PelvisOptions,
                        model => model.PelvisSelection);
                    CreatePartEditor(
                        col, 
                        "Robe",
                        AppearanceArmor.Robe,
                        AppearanceEditorViewModel.ColorTarget.Robe,
                        model => model.RobeOptions,
                        model => model.RobeSelection);

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

                mainRow.AddColumn(col =>
                {
                    CreatePartEditor(
                        col, 
                        "Right Shoulder",
                        AppearanceArmor.RightShoulder,
                        AppearanceEditorViewModel.ColorTarget.RightShoulder,
                        model => model.RightShoulderOptions,
                        model => model.RightShoulderSelection);
                    CreatePartEditor(
                        col, 
                        "Right Bicep",
                        AppearanceArmor.RightBicep,
                        AppearanceEditorViewModel.ColorTarget.RightBicep,
                        model => model.RightBicepOptions,
                        model => model.RightBicepSelection);
                    CreatePartEditor(
                        col, 
                        "Right Forearm",
                        AppearanceArmor.RightForearm,
                        AppearanceEditorViewModel.ColorTarget.RightForearm,
                        model => model.RightForearmOptions,
                        model => model.RightForearmSelection);
                    CreatePartEditor(
                        col, 
                        "Right Hand",
                        AppearanceArmor.RightHand,
                        AppearanceEditorViewModel.ColorTarget.RightHand,
                        model => model.RightHandOptions,
                        model => model.RightHandSelection);
                    CreatePartEditor(
                        col, 
                        "Right Thigh",
                        AppearanceArmor.RightThigh,
                        AppearanceEditorViewModel.ColorTarget.RightThigh,
                        model => model.RightThighOptions,
                        model => model.RightThighSelection);
                    CreatePartEditor(
                        col, 
                        "Right Shin",
                        AppearanceArmor.RightShin,
                        AppearanceEditorViewModel.ColorTarget.RightShin,
                        model => model.RightShinOptions,
                        model => model.RightShinSelection);
                    CreatePartEditor(
                        col, 
                        "Right Foot",
                        AppearanceArmor.RightFoot,
                        AppearanceEditorViewModel.ColorTarget.RightFoot,
                        model => model.RightFootOptions,
                        model => model.RightFootSelection);
                });
            }

            void BuildFooter(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                mainRow.AddColumn(mainCol =>
                {
                    mainCol.AddRow(row =>
                    {
                        row.AddButton()
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
                            .SetText("<-- Copy to Left")
                            .BindIsEnabled(model => model.IsCopyEnabled)
                            .BindOnClicked(model => model.OnClickCopyToLeft());
                    });
                });
            }

            partial.AddColumn(mainCol  =>
            {
                mainCol.AddRow(mainRow =>
                {
                    mainRow.AddGroup(group =>
                    {
                        group.SetShowBorder(false);
                        group.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText(" ")
                                    .SetHeight(20f)
                                    .SetWidth(MainColorChannelButtonSize);
                            });

                            col.AddRow(row =>
                            {
                                row.AddColumn(col2 =>
                                {
                                    col2.AddRow(row2 =>
                                    {
                                        row2.AddPartialView(AppearanceEditorViewModel.ArmorColorElement);
                                    });
                                });

                                row.AddColumn(BuildMainColorChannels);
                            });

                            col.AddRow(row =>
                            {
                                BuildParts(row);
                                row.AddSpacer();
                            });

                            col.AddRow(row =>
                            {
                                BuildFooter(row);
                                row.AddSpacer();
                            });
                        });
                    });
                });

            });
        }

        private void CreateFilledButton(
            GuiExpandableComponent<AppearanceEditorViewModel> component,
            string texture,
            Expression<Func<AppearanceEditorViewModel, GuiRectangle>> regionBind,
            float buttonSize,
            float drawOffset,
            Expression<Func<AppearanceEditorViewModel, Action>> onClickBind,
            GuiRectangle staticRegion = null)
        {
            component.AddButton()
                .SetText("")
                .SetWidth(buttonSize)
                .SetHeight(buttonSize)
                .SetMargin(0f)
                .SetIsEncouraged(true)
                .AddDrawList(drawList =>
                {
                    drawList.AddImage(image =>
                    {
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
        }

        private void BuildSettings(GuiGroup<AppearanceEditorViewModel> partial)
        {
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
                        .SetText("Increase Height")
                        .SetHeight(32f)
                        .SetWidth(128f)
                        .BindOnClicked(model => model.OnIncreaseAppearanceScale());

                    row.AddButton()
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
                        .SetText("Save")
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnClickSaveSettings());
                    row.AddSpacer();
                });
            });
        }

        private void BuildColorPalette(GuiGroup<AppearanceEditorViewModel> group, string texture)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHeight(20f)
                        .BindText(model => model.ColorTargetText)
                        .BindIsVisible(model => model.IsEquipmentSelected);
                });

                const int TextureColorsPerRow = 16;
                const int ColorSize = 16; // 16x16 colors on the sprite sheet
                const int UIColorsPerRow = 20;
                const int ColorTotalCount = 176;
                const int RowCount = 1 + ColorTotalCount / UIColorsPerRow;

                for (var y = 0; y < RowCount; ++y)
                {
                    var yCopy = y;
                    col.AddRow(uiRow =>
                    {
                        for (var x = 0; x < UIColorsPerRow; ++x)
                        {
                            var paletteIndex = yCopy * UIColorsPerRow + x;
                            if (paletteIndex >= ColorTotalCount)
                                break;

                            var row = paletteIndex / TextureColorsPerRow;
                            var offset = paletteIndex % TextureColorsPerRow;

                            var region = new GuiRectangle(
                                offset * ColorSize + 2,
                                row * ColorSize + 2,
                                ColorSize - 4,
                                ColorSize - 4);

                            CreateFilledButton(
                                uiRow, 
                                texture, 
                                null, 
                                PartColorChannelButtonSize, 
                                2f,
                                model => model.OnClickColorPalette(paletteIndex),
                                region);
                        }
                    });
                }

            });
        }
    }
}
