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
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Perks")
                .AddColumn(col =>
                {
                    //// Buy Upgrade confirmation section
                    //col.AddRow(row =>
                    //{
                    //    row.AddLabel()
                    //        .SetText("Are you sure you want to upgrade this perk?");
                    //}).BindIsVisible(model => model.IsConfirmingUpgrade);

                    //// Reset confirmation section
                    //col.AddRow(row =>
                    //{

                    //}).BindIsVisible(model => model.IsConfirmingReset);

                    // Main section - filters, list, buttons, etc.
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        var combo = row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedPerkCategoryId)
                            .SetWidth(200f);

                        combo.AddOption("<All Categories>", 0);
                        foreach (var (type, detail) in Perk.GetAllActivePerkCategories())
                        {
                            combo.AddOption(detail.Name, (int)type);
                        }

                        row.AddSpacer();
                    }).BindIsVisible(model => model.IsInMainView);

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

                    }).BindIsVisible(model => model.IsInMainView);

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.TotalSP)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);
                    }).BindIsVisible(model => model.IsInMainView);

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ResetNextAvailable)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);

                    }).BindIsVisible(model => model.IsInMainView);

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddToggleButton()
                                .BindText(model => model.PerkButtonTexts)
                                .BindIsToggled(model => model.PerkDetailSelected)
                                .BindColor(model => model.PerkButtonColors)
                                .BindOnClicked(model => model.OnSelectPerk());
                        })
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
                                    template2.AddLabel()
                                        .BindText(model => model.SelectedRequirements)
                                        .BindColor(model => model.SelectedRequirementColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetVerticalAlign(NuiVerticalAlign.Top);
                                })
                                    .BindRowCount(model => model.SelectedRequirements)
                                    .BindIsVisible(model => model.IsPerkSelected);
                            });
                        });
                    }).BindIsVisible(model => model.IsInMainView);

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
                    }).BindIsVisible(model => model.IsInMainView);
                });

            return _builder.Build();
        }
    }
}
