using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
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
                .SetInitialGeometry(0, 0, 760f, 620f)
                .BindTitle(model => model.WindowTitle)
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

                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedSortOrderId)
                            .SetWidth(180f)
                            .AddOption("Alphabetical (A-Z)", 0)
                            .AddOption("Alphabetical (Z-A)", 1)
                            .AddOption("Skill Level (Asc)", 2)
                            .AddOption("Skill Level (Desc)", 3);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddToggleButton()
                            .SetText("All")
                            .SetHeight(28f)
                            .BindOnClicked(model => model.OnClickFilterAll())
                            .BindIsToggled(model => model.IsFilterAll);

                        row.AddToggleButton()
                            .SetText("Owned")
                            .SetHeight(28f)
                            .BindOnClicked(model => model.OnClickFilterOwned())
                            .BindIsToggled(model => model.IsFilterOwned);

                        row.AddToggleButton()
                            .SetText("Can Buy")
                            .SetHeight(28f)
                            .BindOnClicked(model => model.OnClickFilterCanBuy())
                            .BindIsToggled(model => model.IsFilterCanBuy);

                        row.AddToggleButton()
                            .SetText("Maxed")
                            .SetHeight(28f)
                            .BindOnClicked(model => model.OnClickFilterMaxed())
                            .BindIsToggled(model => model.IsFilterMaxed);
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

                    col.AddRow(affinityRow =>
                    {
                        affinityRow.SetHeight(88f);
                        affinityRow.BindIsVisible(model => model.IsForceAffinityVisible);
                        affinityRow.AddGroup(group =>
                        {
                            group.SetScrollbars(NuiScrollbars.None);
                            group.AddColumn(affinityColumn =>
                            {
                                affinityColumn.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ForceAffinityHeading)
                                        .BindColor(model => model.ForceAffinityColor)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                                        .SetHeight(24f);
                                });

                                affinityColumn.AddRow(row =>
                                {
                                    row.AddText()
                                        .BindText(model => model.ForceAffinityExplanation)
                                        .SetShowBorder(false)
                                        .SetScrollbars(NuiScrollbars.None)
                                        .SetHeight(58f);
                                });
                            });
                        });
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
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.PerkRowCosts)
                                    .BindColor(model => model.PerkButtonColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);

                                cell.SetWidth(58f);
                                cell.SetIsVariable(false);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.PerkRowReqIcons)
                                        .BindTooltip(model => model.PerkRowReqTooltips)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                                        .SetAspect(NuiAspect.Stretch);
                                });

                                cell.SetWidth(40f);
                                cell.SetIsVariable(false);
                            });
                        })
                            .SetRowHeight(40f)
                            .SetScrollbars(NuiScrollbars.Y)
                            .BindRowCount(model => model.PerkButtonTexts);

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddText()
                                    .BindText(model => model.SelectedDetails)
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
                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.SelectedRequirementIcons)
                                                .BindTooltip(model => model.SelectedRequirementTooltips)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                                .SetAspect(NuiAspect.Stretch);
                                        });

                                        cell.SetWidth(26f);
                                        cell.SetIsVariable(false);
                                    });
                                    template2.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.SelectedRequirements)
                                            .BindColor(model => model.SelectedRequirementColors)
                                            .BindTooltip(model => model.SelectedRequirementTooltips)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                })
                                    .SetRowHeight(28f)
                                    .SetHeight(120f)
                                    .SetScrollbars(NuiScrollbars.Y)
                                    .BindRowCount(model => model.SelectedRequirements)
                                    .BindIsVisible(model => model.IsPerkSelected);
                            });
                        });
                    });

                    col.AddPagination(
                        model => model.PageNumbers,
                        model => model.SelectedPage,
                        model => model.OnClickPreviousPage(),
                        model => model.OnClickNextPage());

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
