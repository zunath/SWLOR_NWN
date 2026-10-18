using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class TechniquesDefinition: IGuiWindowDefinition
    {
        private const float BuildBarHeight = 52f;
        private const float BuildControlHeight = 32f;
        private const float BuildFieldWidth = 260f;
        private const float BuildButtonWidth = 90f;

        private readonly GuiWindowBuilder<TechniquesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Techniques)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 940f, 700f)
                .SetTitle("Techniques")
                .DefinePartialView(TechniquesViewModel.BuildBarPartial, AddBuildBar)
                .DefinePartialView(TechniquesViewModel.ManageBarPartial, AddManageBar)
                .DefinePartialView(TechniquesViewModel.DeleteBarPartial, AddDeleteBar)

                .AddColumn(col =>
                {
                    // Saved-build controls. Manage swaps this bar in place so the lists stay visible.
                    col.AddRow(row =>
                    {
                        row.AddPartialView(TechniquesViewModel.BuildBarElement)
                            .SetHeight(BuildBarHeight);
                    });

                    col.AddRow(row =>
                    {
                        // Both panes share one row shape (title, toolbar, list, action button) so
                        // their lists line up. Neither column is width-pinned, so they split resizes.
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Learned")
                                    .SetHeight(26f);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddTextEdit()
                                    .SetPlaceholder("Search")
                                    .BindValue(model => model.SearchText)
                                    .SetHeight(32f);
                                row2.AddComboBox()
                                    .BindSelectedIndex(model => model.SelectedCategoryId)
                                    .SetWidth(140f)
                                    .SetHeight(32f)
                                    .AddOption("All Types", 0)
                                    .AddOption("Single-Target", 1)
                                    .AddOption("Area", 2)
                                    .AddOption("Stance", 3)
                                    .AddOption("Support", 4)
                                    .AddOption("Passive Trait", 5);
                                row2.AddComboBox()
                                    .BindSelectedIndex(model => model.SelectedSortOrderId)
                                    .SetWidth(140f)
                                    .SetHeight(32f)
                                    .AddOption("Name (A-Z)", 0)
                                    .AddOption("Name (Z-A)", 1)
                                    .AddOption("Rank (Low-High)", 2)
                                    .AddOption("Rank (High-Low)", 3);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.UnequippedIcons)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                                .SetAspect(NuiAspect.Stretch);
                                        });

                                        cell.SetWidth(40f);
                                        cell.SetIsVariable(false);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.UnequippedNames)
                                            .BindIsToggled(model => model.UnequippedSelections)
                                            .BindColor(model => model.UnequippedColors)
                                            .BindOnClicked(model => model.OnSelectUnequipped());
                                    });
                                })
                                .SetRowHeight(40f)
                                .SetScrollbars(NuiScrollbars.Y)
                                .BindRowCount(model => model.UnequippedNames);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer().SetHeight(32f);
                                row2.AddButton()
                                    .SetText("Add to Build")
                                    .SetWidth(140f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsEquipEnabled)
                                    .BindOnClicked(model => model.OnClickEquip());
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.PreviewTitle)
                                    .SetHeight(26f);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.SlotsText)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(90f)
                                    .SetHeight(32f);
                                row2.AddProgressBar()
                                    .BindValue(model => model.SlotsProgress)
                                    .BindColor(model => model.SlotsColor)
                                    .SetHeight(16f);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.EquippedIcons)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                                .SetAspect(NuiAspect.Stretch);
                                        });

                                        cell.SetWidth(40f);
                                        cell.SetIsVariable(false);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.EquippedNames)
                                            .BindIsToggled(model => model.EquippedSelections)
                                            .BindColor(model => model.EquippedColors)
                                            .BindOnClicked(model => model.OnSelectEquipped());
                                    });
                                })
                                .SetRowHeight(40f)
                                .SetScrollbars(NuiScrollbars.Y)
                                .BindRowCount(model => model.EquippedNames);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Remove")
                                    .SetWidth(140f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsUnequipEnabled)
                                    .BindOnClicked(model => model.OnClickUnequip());
                                row2.AddSpacer().SetHeight(32f);
                            });
                        });
                    });

                    // Fixed-height details pane so the lists above take all the flexible vertical space.
                    col.AddRow(row =>
                    {
                        row.SetHeight(140f);
                        row.AddText()
                            .BindText(model => model.SelectedDetails);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetHeight(32f);
                        row.AddButton()
                            .SetText("Revert")
                            .SetWidth(95f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.CanRevert)
                            .BindOnClicked(model => model.OnClickRevert());
                        row.AddButton()
                            .SetText("Apply Build")
                            .SetWidth(135f)
                            .SetHeight(32f)
                            .SetIsEncouraged(true)
                            .BindOnClicked(model => model.OnClickApply());
                    });
                });

            return _builder.Build();
        }

        // The selected build, its save actions, and the equipped build summarized on the right.
        private static void AddBuildBar(GuiGroup<TechniquesViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddComboBox()
                        .BindOptions(model => model.LoadoutOptions)
                        .BindSelectedIndex(model => model.SelectedLoadoutIndex)
                        .SetWidth(BuildFieldWidth)
                        .SetHeight(BuildControlHeight);
                    row.AddButton()
                        .SetText("Save")
                        .SetWidth(80f)
                        .SetHeight(BuildControlHeight)
                        .SetTooltip("Store the preview in the selected build.")
                        .BindOnClicked(model => model.OnClickSave());
                    row.AddButton()
                        .SetText("Save As")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .BindTooltip(model => model.BuildsText)
                        .BindDisabledTooltip(model => model.BuildsText)
                        .BindIsEnabled(model => model.CanSaveAs)
                        .BindOnClicked(model => model.OnClickSaveAs());
                    row.AddButton()
                        .SetText("Manage")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .SetTooltip("Rename or delete the selected build.")
                        .BindOnClicked(model => model.OnClickManage());
                    row.AddLabel()
                        .BindText(model => model.ActiveLoadoutText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right)
                        .SetHeight(BuildControlHeight);
                });
            });
        }

        // The name field takes the dropdown's place, so the build being managed stays in view.
        private static void AddManageBar(GuiGroup<TechniquesViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("Build name")
                        .SetMaxLength(MimicryLoadouts.MaxNameLength)
                        .BindValue(model => model.BuildNameText)
                        .SetWidth(BuildFieldWidth)
                        .SetHeight(BuildControlHeight);
                    row.AddButton()
                        .SetText("Rename")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .SetTooltip("Give the selected build this name.")
                        .BindOnClicked(model => model.OnClickRenameBuild());
                    row.AddButton()
                        .SetText("Delete")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .SetTooltip("Delete the selected build.")
                        .SetDisabledTooltip("Keep at least one saved build.")
                        .BindIsEnabled(model => model.CanDeleteBuild)
                        .BindOnClicked(model => model.OnClickDeleteBuild());
                    row.AddButton()
                        .SetText("Cancel")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .BindOnClicked(model => model.OnClickCancelManage());
                    row.AddLabel()
                        .BindText(model => model.ManageText)
                        .BindColor(model => model.ManageColor)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right)
                        .SetHeight(BuildControlHeight);
                });
            });
        }

        private static void AddDeleteBar(GuiGroup<TechniquesViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.DeleteBuildText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(BuildControlHeight);
                    row.AddButton()
                        .SetText("Delete")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .BindOnClicked(model => model.OnClickConfirmDeleteBuild());
                    row.AddButton()
                        .SetText("Cancel")
                        .SetWidth(BuildButtonWidth)
                        .SetHeight(BuildControlHeight)
                        .BindOnClicked(model => model.OnClickCancelManage());
                });
            });
        }
    }
}
