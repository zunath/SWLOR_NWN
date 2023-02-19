using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PerksDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerksViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Perks)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 600f)
                .SetTitle("Perks")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddToggleButton()
                            .SetText("My Perks")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickMyPerks())
                            .BindIsToggled(model => model.IsInMyPerksMode);

                        row.AddToggleButton()
                            .SetText("Beast Perks")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickBeastPerks())
                            .BindIsEnabled(model => model.HasBeast)
                            .BindIsToggled(model => model.IsInBeastPerksMode);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedPerkCategoryId)
                            .SetWidth(300f)
                            .BindOptions(model => model.Categories);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.AvailableSP)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.TotalSP)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ResetNextAvailable)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.PerkButtonIcons)
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
                                    .BindText(model => model.PerkButtonTexts)
                                    .BindIsToggled(model => model.PerkDetailSelected)
                                    .BindColor(model => model.PerkButtonColors)
                                    .BindOnClicked(model => model.OnSelectPerk());
                            });
                        })
                            .SetRowHeight(40f)
                            .SetScrollbars(NuiScrollbars.Both)
                            .BindRowCount(model => model.PerkButtonTexts);

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddText()
                                    .BindText(model => model.SelectedDetails)
                                    .SetHeight(150f)
                                    .BindIsVisible(model => model.IsPerkSelected);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Requirements")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                    .SetHeight(26f)
                                    .BindIsVisible(model => model.IsPerkSelected);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template2 =>
                                {
                                    template2.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.SelectedRequirements)
                                            .BindColor(model => model.SelectedRequirementColors)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Top);
                                    });
                                })
                                    .SetScrollbars(NuiScrollbars.Both)
                                    .BindRowCount(model => model.SelectedRequirements)
                                    .BindIsVisible(model => model.IsPerkSelected);
                            });
                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickPreviousPage());

                        row.AddComboBox()
                            .BindOptions(model => model.PageNumbers)
                            .BindSelectedIndex(model => model.SelectedPage);

                        row.AddButton()
                            .SetText(">")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickNextPage());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindText(model => model.BuyText)
                            .BindIsEnabled(model => model.IsBuyEnabled)
                            .BindOnClicked(model => model.OnClickBuyUpgrade());

                        row.AddButton()
                            .SetText("Refund")
                            .BindIsEnabled(model => model.IsRefundEnabled)
                            .BindOnClicked(model => model.OnClickRefund());

                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
