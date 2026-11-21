using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public partial class ManageCityDefinition
    {
        private const float SuccessionLabelHeight = 20f;
        private const float SuccessionStatusHeight = 72f;
        private const float SuccessionNoteHeight = 92f;
        private const float SuccessionReplyHeight = 96f;
        private const float SuccessionButtonHeight = 35f;
        private const float SuccessionComboWidth = 340f;

        /// <summary>
        /// The Succession tab: who is Acting Mayor and whether the mayor is stepping down, the mayor's controls to name an
        /// Acting Mayor and to step down, and the named interim holder's Accept and Decline buttons.
        /// </summary>
        private static void AddSuccessionTab(GuiGroup<ManageCityViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetHeight(25f)
                        .BindText(model => model.Instructions)
                        .BindColor(model => model.InstructionsColor);
                });

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.SuccessionStatusText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(SuccessionStatusHeight);
                });

                // The named interim holder answers here.
                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsStepDownReplyVisible);
                    row.AddLabel()
                        .SetText("Step-down waiting for your answer")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(SuccessionLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsStepDownReplyVisible);
                    row.AddText()
                        .BindText(model => model.StepDownReplyText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(SuccessionReplyHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsStepDownReplyVisible);
                    row.AddButton()
                        .SetText("Accept")
                        .SetTooltip("Hold the seat. The mayor steps down and a snap election opens at once.")
                        .BindOnClicked(model => model.AcceptStepDown())
                        .SetHeight(SuccessionButtonHeight);
                    row.AddButton()
                        .SetText("Decline")
                        .SetTooltip("Cancel the mayor's step-down.")
                        .BindOnClicked(model => model.DeclineStepDown())
                        .SetHeight(SuccessionButtonHeight);
                });

                // The mayor's controls.
                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddLabel()
                        .SetText("Acting Mayor")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(SuccessionLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddText()
                        .SetText("Name a citizen in good standing Acting Mayor for up to 30 days, for example while you are away. They can decide City Affairs, " +
                                 "change taxes, pay upkeep, start projects, enact Edicts into empty slots, and post requests. They cannot withdraw from the treasury, " +
                                 "repeal Edicts, appoint, or grant permissions. The term ends when you end it, after the time you pick, when they leave the city, or when an election seats a mayor.")
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(SuccessionNoteHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddComboBox()
                        .BindOptions(model => model.ActingCandidateOptions)
                        .BindSelectedIndex(model => model.SelectedActingCandidateIndex)
                        .BindIsEnabled(model => model.CanNameActingMayor)
                        .SetTooltip("Citizens who owe no taxes.")
                        .SetWidth(SuccessionComboWidth)
                        .SetHeight(SuccessionButtonHeight);
                    row.AddComboBox()
                        .BindOptions(model => model.ActingTermOptions)
                        .BindSelectedIndex(model => model.SelectedActingTermIndex)
                        .BindIsEnabled(model => model.CanNameActingMayor)
                        .SetTooltip("How long the term lasts. The longest is 30 days.")
                        .SetHeight(SuccessionButtonHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddButton()
                        .SetText("Name Acting Mayor")
                        .BindIsEnabled(model => model.CanNameActingMayor)
                        .SetTooltip("Names the citizen above Acting Mayor for the term you picked. The Gazette announces it.")
                        .BindOnClicked(model => model.NameActingMayor())
                        .SetHeight(SuccessionButtonHeight);
                    row.AddButton()
                        .SetText("End Term")
                        .BindIsEnabled(model => model.CanEndActingMayor)
                        .SetTooltip("Ends the Acting Mayor's term now. Anything they did stays done.")
                        .BindOnClicked(model => model.EndActingMayor())
                        .SetHeight(SuccessionButtonHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddLabel()
                        .SetText("Step Down")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(SuccessionLabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddText()
                        .BindText(model => model.StepDownHelpText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(SuccessionNoteHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddComboBox()
                        .BindOptions(model => model.InterimHolderOptions)
                        .BindSelectedIndex(model => model.SelectedInterimHolderIndex)
                        .BindIsEnabled(model => model.CanStepDown)
                        .SetTooltip("Your Acting Mayor or one of your officers.")
                        .SetWidth(SuccessionComboWidth)
                        .SetHeight(SuccessionButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayorSuccessionVisible);
                    row.AddButton()
                        .SetText("Step Down")
                        .BindIsEnabled(model => model.CanStepDown)
                        .SetTooltip("Asks the player above to hold the seat. They have 7 days to accept.")
                        .BindOnClicked(model => model.StepDown())
                        .SetHeight(SuccessionButtonHeight);
                    row.AddButton()
                        .SetText("Withdraw")
                        .BindIsEnabled(model => model.CanCancelStepDown)
                        .SetTooltip("Cancels a step-down the named player has not yet accepted.")
                        .BindOnClicked(model => model.CancelStepDown())
                        .SetHeight(SuccessionButtonHeight);
                });
            });
        }
    }
}
