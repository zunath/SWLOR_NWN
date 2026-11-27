using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ElectionDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ElectionViewModel> _builder = new();

        // Fits a 1280x720 client with the title bar visible (a taller window opens with its top edge off the screen). Each
        // step's panel scrolls, so the platform step, taller than the window, stays reachable down to its buttons.
        private const float WindowWidth = 580f;
        private const float WindowHeight = 640f;

        private const float ContentPanelWidth = 520f;
        private const float LabelHeight = 20f;
        private const float InstructionsHeight = 130f;
        private const float CandidateListHeight = 110f;
        private const float CandidateRowHeight = 28f;
        private const float CandidateDetailsHeight = 200f;
        private const float StatementEditorHeight = 100f;
        private const float ButtonHeight = 35f;
        private const float PlatformInstructionsHeight = 92f;
        private const float PlatformEdictRowHeight = 28f;
        private const float PlatformEdictListHeight = 140f;
        private const float FocusedEdictHeight = 56f;
        private const float PlatformTargetRowHeight = 28f;
        private const float PlatformTargetListHeight = 84f;
        private const float PlatformMessageHeight = 40f;
        private const float CostPreviewHeight = 150f;
        private const float CityLevelWarningHeight = 100f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Election)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Election")
                .DefinePartialView(ElectionViewModel.MainContentPartial, AddMainContent)
                .DefinePartialView(ElectionViewModel.PlatformPartial, AddPlatformContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(ElectionViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<ElectionViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(content =>
                        {
                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.Instructions)
                                    .SetHeight(InstructionsHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.ScheduleText)
                                    .SetColor(GuiColor.Cyan)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .SetText("Candidates")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindIsToggled(model => model.CandidateToggles)
                                                .BindText(model => model.CandidateNames)
                                                .BindIsEnabled(model => model.CandidateEnables)
                                                .BindOnClicked(model => model.SelectCandidate());
                                        });
                                    })
                                    .BindRowCount(model => model.CandidateNames)
                                    .SetRowHeight(CandidateRowHeight)
                                    .SetHeight(CandidateListHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .SetText("Platform")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.SelectedCandidateDetails)
                                    .SetHeight(CandidateDetailsHeight);
                            });

                            // Registration: a citizen enters the race, or edits their platform, in the platform step.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsPlatformButtonVisible);
                                line.AddSpacer();
                                line.AddButton()
                                    .BindText(model => model.PlatformButtonText)
                                    .SetTooltip("Choose the Edicts you promise, see what they cost, and write your statement. You can edit your platform until registration closes.")
                                    .BindOnClicked(model => model.OpenPlatform())
                                    .SetHeight(ButtonHeight);
                                line.AddSpacer();
                            });

                            // Voting: the player's current vote.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsVoteStatusVisible);
                                line.AddLabel()
                                    .BindText(model => model.VoteStatusText)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            // Registration (Exit Race, for a candidate) and a contested vote (Cast Vote).
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsMainActionVisible);
                                line.AddSpacer();
                                line.AddButton()
                                    .BindText(model => model.MainActionButtonText)
                                    .BindColor(model => model.MainActionButtonColor)
                                    .BindOnClicked(model => model.MainAction())
                                    .SetHeight(ButtonHeight);
                                line.AddSpacer();
                            });

                            // A confidence vote on a lone candidate.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsConfidenceVoteVisible);
                                line.AddButton()
                                    .SetText("Vote Yes")
                                    .SetTooltip("Vote Yes: the candidate becomes mayor if Yes wins.")
                                    .BindOnClicked(model => model.VoteYes())
                                    .SetHeight(ButtonHeight);
                                line.AddButton()
                                    .SetText("Vote No")
                                    .SetTooltip("Vote No: the candidate does not become mayor if No wins.")
                                    .BindOnClicked(model => model.VoteNo())
                                    .SetHeight(ButtonHeight);
                                line.AddButton()
                                    .SetText("Clear Vote")
                                    .SetTooltip("Take your vote back. Clearing a vote never pays the voter reward again.")
                                    .BindOnClicked(model => model.ClearVote())
                                    .SetHeight(ButtonHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        /// <summary>
        /// The platform step: the Edicts a candidate promises, what they cost beside the city's upkeep and treasury, the
        /// City Management warning, and the statement.
        /// </summary>
        private static void AddPlatformContent(GuiGroup<ElectionViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(content =>
                        {
                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.PlatformInstructions)
                                    .SetHeight(PlatformInstructionsHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.PlatformSlotsText)
                                    .SetColor(GuiColor.Cyan)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindIsToggled(model => model.PlatformEdictToggles)
                                                .BindText(model => model.PlatformEdictNames)
                                                .BindTooltip(model => model.PlatformEdictTooltips)
                                                .BindIsEnabled(model => model.PlatformEdictEnables)
                                                .BindOnClicked(model => model.TogglePlatformEdict());
                                        });
                                    })
                                    .BindRowCount(model => model.PlatformEdictNames)
                                    .SetRowHeight(PlatformEdictRowHeight)
                                    .SetHeight(PlatformEdictListHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.FocusedEdictText)
                                    .SetHeight(FocusedEdictHeight);
                            });

                            // An Alliance or Embargo in the platform names a city.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsPlatformTargetPickerVisible);
                                line.AddLabel()
                                    .BindText(model => model.PlatformTargetPrompt)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsPlatformTargetPickerVisible);
                                line.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindIsToggled(model => model.PlatformTargetToggles)
                                                .BindText(model => model.PlatformTargetNames)
                                                .BindOnClicked(model => model.SelectPlatformTarget());
                                        });
                                    })
                                    .BindRowCount(model => model.PlatformTargetNames)
                                    .SetRowHeight(PlatformTargetRowHeight)
                                    .SetHeight(PlatformTargetListHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsPlatformMessageVisible);
                                line.AddText()
                                    .BindText(model => model.PlatformMessage)
                                    .SetHeight(PlatformMessageHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .SetText("What your platform costs")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.CostPreviewText)
                                    .SetHeight(CostPreviewHeight);
                            });

                            // Shown only when the candidate's City Management rank would lower the city.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsCityLevelWarningVisible);
                                line.AddText()
                                    .BindText(model => model.CityLevelWarningText)
                                    .SetHeight(CityLevelWarningHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .SetText($"Your statement ({ElectionRules.StatementMaxLength} characters at most)")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddTextEdit()
                                    .SetPlaceholder("Tell citizens what you stand for.")
                                    .SetMaxLength(ElectionRules.StatementMaxLength)
                                    .SetIsMultiline(true)
                                    .BindValue(model => model.StatementInput)
                                    .SetHeight(StatementEditorHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Back")
                                    .SetTooltip("Return to the candidates without saving.")
                                    .BindOnClicked(model => model.ClosePlatform())
                                    .SetHeight(ButtonHeight);
                                line.AddButton()
                                    .BindText(model => model.PlatformSubmitText)
                                    .SetTooltip("Enter the race with this platform, or save the changes to your platform. You can edit it until registration closes.")
                                    .BindOnClicked(model => model.SubmitPlatform())
                                    .SetHeight(ButtonHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }
    }
}
