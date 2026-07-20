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
                .SetInitialGeometry(0, 0, 560f, 600f)
                .SetTitle("Techniques")

                .AddColumn(col =>
                {
                    // Category filter + sort order, filtering/ordering the Learned list. Fixed-width
                    // combo boxes centered with flexing spacers (the Perks/Key Items pattern) so this
                    // row fills the window without pinning the content width.
                    col.AddRow(row =>
                    {
                        row.SetHeight(32f);
                        row.AddSpacer();
                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedCategoryId)
                            .SetWidth(240f)
                            .AddOption("All Types", 0)
                            .AddOption("Single-Target", 1)
                            .AddOption("Area", 2)
                            .AddOption("Stance", 3)
                            .AddOption("Support", 4)
                            .AddOption("Passive Trait", 5);

                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedSortOrderId)
                            .SetWidth(180f)
                            .AddOption("Name (A-Z)", 0)
                            .AddOption("Name (Z-A)", 1)
                            .AddOption("Rank (Low-High)", 2)
                            .AddOption("Rank (High-Low)", 3);
                        row.AddSpacer();
                    });

                    // Search box (filters the Learned list by technique name as you type).
                    col.AddRow(row =>
                    {
                        row.SetHeight(30f);
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);
                    });

                    col.AddRow(row =>
                    {
                        // Learned list column: no fixed width, so it flexes to fill the space left
                        // by the fixed-width button column and shares it evenly with the Equipped column.
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
                        });

                        // Fixed-width control column between the two lists. Pinning its width keeps
                        // the buttons compact and lets the flanking lists claim the rest of the row.
                        // The buttons carry their own fixed width and are centered with spacers so
                        // they never overflow into the Equipped list on the right.
                        row.AddColumn(col2 =>
                        {
                            col2.SetWidth(130f);
                            col2.AddRow(row2 => row2.AddSpacer());
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .SetText("Equip ->")
                                    .SetHeight(32f)
                                    .SetWidth(110f)
                                    .BindIsEnabled(model => model.IsEquipEnabled)
                                    .BindOnClicked(model => model.OnClickEquip());
                                row2.AddSpacer();
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .SetText("<- Unequip")
                                    .SetHeight(32f)
                                    .SetWidth(110f)
                                    .BindIsEnabled(model => model.IsUnequipEnabled)
                                    .BindOnClicked(model => model.OnClickUnequip());
                                row2.AddSpacer();
                            });
                            col2.AddRow(row2 => row2.AddSpacer());
                        });

                        // Equipped list column: also flexes to fill the remaining space.
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Equipped")
                                    .SetHeight(26f);
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
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.SlotsText)
                            .BindColor(model => model.SlotsColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(26f);
                        row.AddSpacer();
                    });

                    // Visual slot-usage bar beneath the "Slots: x / y" label.
                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.SlotsProgress)
                            .BindColor(model => model.SlotsColor)
                            .SetHeight(16f);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
