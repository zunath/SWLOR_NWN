using NRediSearch.Aggregation;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        private const float MainColorChannelButtonSize = 70f;
        private const float PartColorChannelButtonSize = 18f;

        private GuiRectangle DummyRegion = new GuiRectangle(0, 0, 16, 16);

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
                    BuildColors(partial, "gui_pal_tattoo");
                })

                .DefinePartialView(AppearanceEditorViewModel.ArmorColorsMetal, partial =>
                {
                    BuildColors(partial, "gui_pal_armor01");
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
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                });
                col.AddRow(row =>
                {
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, MainColorChannelButtonSize, 4f);
                });
                col.AddRow(row =>
                {
                    row.AddSpacer();
                });
            }

            void CreatePartEditor(GuiColumn<AppearanceEditorViewModel> col, string partName)
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
                        .SetMargin(0f);

                    row.AddComboBox()
                        .SetHeight(24f)
                        .SetWidth(100f)
                        .SetMargin(0f)
                        .AddOption("1", 1);

                    row.AddButton()
                        .SetText(">")
                        .SetHeight(24f)
                        .SetWidth(24f)
                        .SetMargin(0f);
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
                    CreateFilledButton(row, "gui_pal_armor01", DummyRegion, PartColorChannelButtonSize, 2f);
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
                    CreatePartEditor(col, "Left Shoulder");
                    CreatePartEditor(col, "Left Bicep");
                    CreatePartEditor(col, "Left Forearm");
                    CreatePartEditor(col, "Left Hand");
                    CreatePartEditor(col, "Left Thigh");
                    CreatePartEditor(col, "Left Shin");
                    CreatePartEditor(col, "Left Foot");
                });

                CreateGap(mainRow);

                mainRow.AddColumn(col =>
                {
                    CreatePartEditor(col, "Neck");
                    CreatePartEditor(col, "Chest");
                    CreatePartEditor(col, "Belt");
                    CreatePartEditor(col, "Pelvis");
                    CreatePartEditor(col, "Robe");

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
                    CreatePartEditor(col, "Right Shoulder");
                    CreatePartEditor(col, "Right Bicep");
                    CreatePartEditor(col, "Right Forearm");
                    CreatePartEditor(col, "Right Hand");
                    CreatePartEditor(col, "Right Thigh");
                    CreatePartEditor(col, "Right Shin");
                    CreatePartEditor(col, "Right Foot");
                });
            }

            void BuildFooter(GuiRow<AppearanceEditorViewModel> mainRow)
            {
                mainRow.AddColumn(mainCol =>
                {
                    mainCol.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Copy to Right -->");
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
                            .SetText("<-- Copy to Left");
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
            GuiRectangle region,
            float buttonSize,
            float drawOffset)
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
                        image.SetDrawTextureRegion(region);
                    });
                });
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

        private void BuildColors(GuiGroup<AppearanceEditorViewModel> group, string texture)
        {
            group.AddColumn(col =>
            {
                const int ColorsPerRow = 16;
                const int RowCount = 11;
                const int ColorSize = 16; // 16x16 colors on the sprite sheet

                for (var rowIndex = 0; rowIndex < RowCount; rowIndex++)
                {
                    var y = ColorSize * rowIndex;

                    col.AddRow(row =>
                    {
                        for (var columnIndex = 0; columnIndex < ColorsPerRow; columnIndex++)
                        {
                            var x = ColorSize * columnIndex;
                            var region = new GuiRectangle(x, y, ColorSize, ColorSize);

                            CreateFilledButton(row, texture, region, PartColorChannelButtonSize, 2f);
                        }

                    });

                }
            });
        }
    }
}
