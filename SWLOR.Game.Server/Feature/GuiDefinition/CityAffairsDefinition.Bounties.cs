using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The Bounties tab of City Affairs: the city's open bounties, withdrawing one, and posting a bounty on an online player
    /// who shows the PvP flag.
    /// </summary>
    public partial class CityAffairsDefinition
    {
        private const float BountyRowHeight = 28f;
        private const float BountyListHeight = 150f;
        private const float BountyNoteHeight = 56f;
        private const float RewardWidth = 200f;

        private static void AddBountiesTab(GuiGroup<CityAffairsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddInstructions(content);

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.BountyStatusText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(BountyNoteHeight);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Open bounties")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.BountyNames)
                                    .BindIsToggled(model => model.BountyToggles)
                                    .BindOnClicked(model => model.SelectBounty());
                            });
                        })
                        .BindRowCount(model => model.BountyNames)
                        .SetRowHeight(BountyRowHeight);
                    row.SetHeight(BountyListHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsBountyListEmpty);
                    row.AddLabel()
                        .SetText("The city has no open bounties.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Withdraw Bounty")
                        .BindIsEnabled(model => model.CanWithdrawBounty)
                        .SetTooltip("Withdraws the bounty picked in the list. The reward returns to the treasury.")
                        .BindOnClicked(model => model.OnClickWithdrawBounty())
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Post a bounty")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText($"Online players showing the PvP flag. Rewards start at {BountyRules.FormatCredits(BountyRules.MinimumReward)} credits.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddComboBox()
                        .BindOptions(model => model.TargetOptions)
                        .BindSelectedIndex(model => model.SelectedTargetIndex)
                        .BindIsEnabled(model => model.CanPostBounty)
                        .SetTooltip("Players you can see in the Player List with the PvP flag. A disguise counts as its own identity.")
                        .SetWidth(ComboWidth)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.TargetStatusText)
                        .BindColor(model => model.TargetStatusColor)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddTextEdit()
                        .BindValue(model => model.RewardInput)
                        .BindIsEnabled(model => model.CanPostBounty)
                        .SetPlaceholder("Reward in credits")
                        .SetMaxLength(12)
                        .SetWidth(RewardWidth);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Post Bounty")
                        .BindIsEnabled(model => model.CanPostBounty)
                        .SetTooltip("Holds the reward out of the treasury until someone collects it or it runs out after 7 days. The bounty is public in the Gazette, and the player is told.")
                        .BindOnClicked(model => model.OnClickPostBounty())
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });
            });
        }
    }
}
