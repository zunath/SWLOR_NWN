using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class TechniquesDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TechniquesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Techniques)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 940f, 700f)
                .SetTitle("Techniques")

                .AddColumn(col =>
                {
                    // Saved-build controls, with the equipped build summarized on the right.
                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindOptions(model => model.LoadoutOptions)
                            .BindSelectedIndex(model => model.SelectedLoadoutIndex)
                            .SetWidth(260f)
                            .SetHeight(32f);
                        row.AddButton()
                            .SetText("Save")
                            .SetWidth(80f)
                            .SetHeight(32f)
                            .SetTooltip("Store the preview in the selected build.")
                            .BindOnClicked(model => model.OnClickSave());
                        row.AddButton()
                            .SetText("Save As")
                            .SetWidth(90f)
                            .SetHeight(32f)
                            .BindTooltip(model => model.BuildsText)
                            .BindDisabledTooltip(model => model.BuildsText)
                            .BindIsEnabled(model => model.CanSaveAs)
                            .BindOnClicked(model => model.OnClickSaveAs());
                        row.AddButton()
                            .SetText("Manage")
                            .SetWidth(90f)
                            .SetHeight(32f)
                            .SetTooltip("Rename or delete the selected build.")
                            .BindOnClicked(model => model.OnClickManage());
                        row.AddLabel()
                            .BindText(model => model.ActiveLoadoutText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right)
                            .SetHeight(32f);
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
    }
}
