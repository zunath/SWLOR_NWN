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
                .SetInitialGeometry(0, 0, 560f, 540f)
                .SetTitle("Techniques")

                .AddColumn(col =>
                {
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
                                        cell.AddToggleButton()
                                            .BindText(model => model.UnequippedNames)
                                            .BindIsToggled(model => model.UnequippedSelections)
                                            .BindColor(model => model.UnequippedColors)
                                            .BindOnClicked(model => model.OnSelectUnequipped());
                                    });
                                })
                                .SetRowHeight(30f)
                                .SetScrollbars(NuiScrollbars.Both)
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
                                        cell.AddToggleButton()
                                            .BindText(model => model.EquippedNames)
                                            .BindIsToggled(model => model.EquippedSelections)
                                            .BindColor(model => model.EquippedColors)
                                            .BindOnClicked(model => model.OnSelectEquipped());
                                    });
                                })
                                .SetRowHeight(30f)
                                .SetScrollbars(NuiScrollbars.Both)
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

                    // Slot budget shown both as text and as a visual fill bar (green with room,
                    // red when full).
                    col.AddRow(row =>
                    {
                        row.SetHeight(22f);
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.SlotsText)
                            .BindColor(model => model.SlotsColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(22f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.SetHeight(16f);
                        row.AddProgressBar()
                            .BindValue(model => model.SlotsProgress)
                            .BindColor(model => model.SlotsColor);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
