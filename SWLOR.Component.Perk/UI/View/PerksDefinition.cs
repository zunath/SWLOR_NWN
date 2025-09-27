using SWLOR.Component.Perk.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Perk.UI.View
{
    public class PerksDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<PerksViewModel> _builder;

        public PerksDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<PerksViewModel>(_guiService);
        }

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
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHeight(26f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.TotalSP)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ResetNextAvailable)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
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
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                                        .SetVerticalAlign(NuiVerticalAlignType.Middle)
                                        .SetAspect(NuiAspectType.Stretch);
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
                            .SetScrollbars(NuiScrollbarType.Both)
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
                                    .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                                    .SetVerticalAlign(NuiVerticalAlignType.Top)
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
                                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                            .SetVerticalAlign(NuiVerticalAlignType.Top);
                                    });
                                })
                                    .SetScrollbars(NuiScrollbarType.Both)
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
