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
                                            .BindOnClicked(model => model.OnSelectUnequipped());
                                    });
                                })
                                .SetRowHeight(30f)
                                .SetScrollbars(NuiScrollbars.Both)
                                .BindRowCount(model => model.UnequippedNames)
                                .SetWidth(220f);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 => row2.AddSpacer());
                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Equip ->")
                                    .SetHeight(32f)
                                    .SetWidth(96f)
                                    .BindIsEnabled(model => model.IsEquipEnabled)
                                    .BindOnClicked(model => model.OnClickEquip());
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("<- Unequip")
                                    .SetHeight(32f)
                                    .SetWidth(96f)
                                    .BindIsEnabled(model => model.IsUnequipEnabled)
                                    .BindOnClicked(model => model.OnClickUnequip());
                            });
                            col2.AddRow(row2 => row2.AddSpacer());
                        });

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
                                            .BindOnClicked(model => model.OnSelectEquipped());
                                    });
                                })
                                .SetRowHeight(30f)
                                .SetScrollbars(NuiScrollbars.Both)
                                .BindRowCount(model => model.EquippedNames)
                                .SetWidth(220f);
                            });
                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.SelectedDetails)
                            .SetHeight(140f);
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
                })
                ;

            return _builder.Build();
        }
    }
}
