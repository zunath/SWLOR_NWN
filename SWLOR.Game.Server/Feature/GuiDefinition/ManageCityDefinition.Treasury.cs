using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public partial class ManageCityDefinition
    {
        private const float TreasuryRowHeight = 44f;
        private const float TreasuryTimeWidth = 96f;
        private const float TreasuryAmountWidth = 92f;
        private const float TreasuryListHeight = 380f;

        /// <summary>
        /// The Treasury tab: the last 30 days of changes to the treasury, newest first, for those who may withdraw from it.
        /// Each line shows when, the change and what made it; its tooltip adds the balance after it.
        /// </summary>
        private static void AddTreasuryTab(GuiGroup<ManageCityViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetHeight(25f)
                        .BindText(model => model.TreasuryHistoryHeader)
                        .BindColor(model => model.TreasuryHistoryHeaderColor);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsTreasuryHistoryVisible);
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(TreasuryTimeWidth);
                                cell.AddLabel()
                                    .BindText(model => model.TreasuryHistoryTimes)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(TreasuryAmountWidth);
                                cell.AddLabel()
                                    .BindText(model => model.TreasuryHistoryAmounts)
                                    .BindColor(model => model.TreasuryHistoryAmountColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            // A long line (a bounty naming two people) wraps onto a second line rather than being cut off.
                            template.AddCell(cell =>
                            {
                                cell.AddText()
                                    .BindText(model => model.TreasuryHistoryTexts)
                                    .BindTooltip(model => model.TreasuryHistoryTooltips)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(TreasuryRowHeight - 4f);
                            });
                        })
                        .BindRowCount(model => model.TreasuryHistoryTimes)
                        .SetRowHeight(TreasuryRowHeight)
                        .SetHeight(TreasuryListHeight);
                });
            });
        }
    }
}
