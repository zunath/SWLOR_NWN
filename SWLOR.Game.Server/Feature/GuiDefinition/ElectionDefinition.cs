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

        private const float ContentPanelWidth = 520f;
        private const float LabelHeight = 20f;
        private const float InstructionsHeight = 100f;
        private const float CandidateListHeight = 110f;
        private const float CandidateRowHeight = 28f;
        private const float StatementHeight = 110f;
        private const float StatementEditorHeight = 100f;
        private const float ButtonHeight = 35f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Election)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 580f, 760f)
                .SetTitle("Election")
                .DefinePartialView(ElectionViewModel.MainContentPartial, AddMainContent);

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
                        panel.SetScrollbars(NuiScrollbars.None);
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
                                    .SetText("Statement")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.SelectedStatement)
                                    .SetHeight(StatementHeight);
                            });

                            // Registration: a citizen writes or edits their own statement.
                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsStatementEditorVisible);
                                line.AddLabel()
                                    .SetText($"Your statement ({ElectionRules.StatementMaxLength} characters at most)")
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsStatementEditorVisible);
                                line.AddTextEdit()
                                    .SetPlaceholder("Tell citizens what you stand for.")
                                    .SetMaxLength(ElectionRules.StatementMaxLength)
                                    .SetIsMultiline(true)
                                    .BindValue(model => model.StatementInput)
                                    .SetHeight(StatementEditorHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.BindIsVisible(model => model.IsSaveStatementVisible);
                                line.AddButton()
                                    .SetText("Save Statement")
                                    .SetTooltip("Save the statement shown above. You can edit it until registration closes.")
                                    .BindOnClicked(model => model.SaveStatement())
                                    .SetHeight(ButtonHeight);
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

                            // Registration (Enter or Exit Race) and a contested vote (Cast Vote).
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
    }
}
