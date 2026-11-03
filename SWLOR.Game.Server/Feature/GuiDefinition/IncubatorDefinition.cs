using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class IncubatorDefinition : IGuiWindowDefinition
    {
        private const float ContentPanelWidth = 560f;
        private const float MutationRailWidth = 300f;
        private const float StageBoxHeight = 56f;
        private const float StageBoxWidth = 178f;
        private const float PurityRowHeight = 22f;
        private const float PurityRowPitch = 26f;
        private const int CollapsedPurityRows = 8;

        private readonly GuiWindowBuilder<IncubatorViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Incubator)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 920f, 690f)
                .SetTitle("Incubator")
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(IncubatorViewModel.NewJobPartial, host => AddBody(host, BuildNewJobSections))
                .DefinePartialView(IncubatorViewModel.InProgressJobPartial, host => AddBody(host, BuildInProgressSections))
                .DefinePartialView(IncubatorViewModel.StageCompleteJobPartial, host => AddBody(host, BuildStageCompleteSections))
                .DefinePartialView(IncubatorViewModel.CompleteJobPartial, host => AddBody(host, BuildCompleteSections));

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(IncubatorViewModel.PartialElement);
                layout.AddSideColumn(BuildMutationRail, MutationRailWidth);
            });

            return _builder.Build();
        }

        private static void AddBody(GuiGroup<IncubatorViewModel> host, System.Action<GuiColumn<IncubatorViewModel>> buildSections)
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
                            BuildHeader(content);
                            BuildStageTracker(content);
                            buildSections(content);
                        });
                    }).SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void BuildNewJobSections(GuiColumn<IncubatorViewModel> col)
        {
            BuildEnzymeSlots(col);
            BuildPurities(col);
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.EstimatedTimeToCompletion)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddButton()
                    .SetText("Start stage 1")
                    .SetWidth(150f)
                    .BindOnClicked(model => model.OnClickStartJob())
                    .BindIsEnabled(model => model.IsStartJobEnabled)
                    .SetDisabledTooltip("Add DNA and all three enzymes first.");
            });
        }

        private static void BuildStageCompleteSections(GuiColumn<IncubatorViewModel> col)
        {
            BuildEnzymeSlots(col);
            BuildPurities(col);
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.EstimatedTimeToCompletion)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddButton()
                    .SetText("Cancel job")
                    .SetWidth(120f)
                    .BindOnClicked(model => model.OnClickCancelJob());
                row.AddButton()
                    .BindText(model => model.ContinueButtonText)
                    .SetWidth(150f)
                    .BindOnClicked(model => model.OnClickContinueJob())
                    .BindIsEnabled(model => model.IsContinueJobEnabled)
                    .SetDisabledTooltip("Add all three enzymes first.");
            });
        }

        private static void BuildInProgressSections(GuiColumn<IncubatorViewModel> col)
        {
            BuildJobProgress(col);
            BuildPurities(col);
            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetText("Cancel job")
                    .SetWidth(120f)
                    .BindOnClicked(model => model.OnClickCancelJob());
            });
        }

        private static void BuildCompleteSections(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.OutcomeHeader)
                    .SetHeight(22f)
                    .SetColor(GuiColor.Cyan)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddButton()
                    .SetText("Complete job")
                    .SetWidth(150f)
                    .SetTooltip("Hatch the egg. Shows the outcome odds before you confirm.")
                    .BindOnClicked(model => model.OnClickCompleteJob())
                    .BindIsEnabled(model => model.IsCompleteJobEnabled);
            });
            col.AddRow(row =>
            {
                row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.OutcomeNames)
                                .BindColor(model => model.OutcomeColors)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.OutcomeChances)
                                .BindColor(model => model.OutcomeColors)
                                .SetWidth(80f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        });
                    })
                    .BindRowCount(model => model.OutcomeNames)
                    .SetRowHeight(24f)
                    .SetHeight(84f);
            });
            BuildPurities(col);
        }

        private static void BuildHeader(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddButtonImage()
                    .BindImageResref(model => model.DNAItemResref)
                    .BindOnClicked(model => model.OnClickDNA())
                    .BindIsEnabled(model => model.IsDNASlotEnabled)
                    .BindTooltip(model => model.DNASlotTooltip)
                    .SetHeight(40f)
                    .SetWidth(40f);

                row.AddColumn(text =>
                {
                    text.AddRow(r => r.AddLabel()
                        .BindText(model => model.BeastName)
                        .SetHeight(22f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    text.AddRow(r => r.AddLabel()
                        .BindText(model => model.BeastSubtitle)
                        .SetColor(GuiColor.Grey)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                });

                row.AddLabel()
                    .BindText(model => model.ServerDayText)
                    .SetColor(GuiColor.Grey)
                    .SetWidth(210f)
                    .SetHeight(40f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right);
            });
        }

        private static void BuildStageTracker(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                AddStageBox(row, model => model.Stage1Title, model => model.Stage1Color, model => model.Stage1Detail);
                AddStageBox(row, model => model.Stage2Title, model => model.Stage2Color, model => model.Stage2Detail);
                AddStageBox(row, model => model.Stage3Title, model => model.Stage3Color, model => model.Stage3Detail);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.EnzymeTotalsText)
                    .SetColor(GuiColor.Grey)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddStageBox(
            GuiRow<IncubatorViewModel> row,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, string>> title,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, GuiColor>> color,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, string>> detail)
        {
            row.AddGroup(group =>
            {
                group.SetShowBorder(true);
                group.SetScrollbars(NuiScrollbars.None);
                group.AddColumn(box =>
                {
                    box.AddRow(r => r.AddLabel()
                        .BindText(title)
                        .BindColor(color)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    box.AddRow(r => r.AddLabel()
                        .BindText(detail)
                        .SetColor(GuiColor.Grey)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                });
            }).SetHeight(StageBoxHeight).SetWidth(StageBoxWidth);
        }

        private static void BuildEnzymeSlots(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.EnzymeSectionTitle)
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                AddEnzymeSlot(row, "Hydrolase",
                    model => model.HydrolaseTitle,
                    model => model.HydrolaseItemResref,
                    model => model.OnClickHydrolase(),
                    model => model.HydrolaseDetail);
                AddEnzymeSlot(row, "Lyase",
                    model => model.LyaseTitle,
                    model => model.LyaseItemResref,
                    model => model.OnClickLyase(),
                    model => model.LyaseDetail);
                AddEnzymeSlot(row, "Isomerase",
                    model => model.IsomeraseTitle,
                    model => model.IsomeraseItemResref,
                    model => model.OnClickIsomerase(),
                    model => model.IsomeraseDetail);
            });
        }

        private static void AddEnzymeSlot(
            GuiRow<IncubatorViewModel> row,
            string name,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, string>> title,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, string>> icon,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, System.Action>> onClick,
            System.Linq.Expressions.Expression<System.Func<IncubatorViewModel, string>> detail)
        {
            row.AddButtonImage()
                .BindImageResref(icon)
                .BindOnClicked(onClick)
                .SetTooltip($"Add or remove a {name} enzyme")
                .SetHeight(40f)
                .SetWidth(40f);

            row.AddColumn(text =>
            {
                text.AddRow(r => r.AddLabel()
                    .BindText(title)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left));
                text.AddRow(r => r.AddLabel()
                    .BindText(detail)
                    .BindTooltip(detail)
                    .SetColor(GuiColor.Grey)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left));
            });
        }

        private static void BuildJobProgress(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(model => model.JobProgress)
                    .SetColor(0, 120, 0)
                    .AddDrawList(list =>
                    {
                        list.AddText(text =>
                        {
                            text.SetColor(255, 255, 255);
                            text.SetBounds(20, 10, 400, 100);
                            text.BindText(model => model.JobProgressTime);
                        });
                    });
            });
        }

        private static void BuildPurities(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.PurityHeader)
                    .SetTooltip("The number in brackets is this stage's gain. Each stat gains at most 10% per stage.")
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddButton()
                    .BindText(model => model.ResistanceToggleText)
                    .BindOnClicked(model => model.OnClickToggleResistances())
                    .SetWidth(150f);
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.PurityNames)
                                .BindColor(model => model.PurityNameColors)
                                .BindTooltip(model => model.PurityTooltips)
                                .SetWidth(110f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.AddProgressBar()
                                .BindValue(model => model.PurityValues);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.PurityTexts)
                                .BindColor(model => model.PurityNameColors)
                                .SetWidth(110f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        });
                    })
                    .BindRowCount(model => model.PurityNames)
                    .SetRowHeight(PurityRowHeight)
                    .SetHeight(PurityRowPitch * CollapsedPurityRows + 12f);
            });
        }

        private static void BuildMutationRail(GuiColumn<IncubatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Mutation")
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.MutationChanceText)
                    .BindColor(model => model.MutationChanceColor)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddCheckBox()
                    .SetText(" Allow mutation")
                    .BindIsChecked(model => model.IsMutationAllowed)
                    .SetTooltip("When unchecked, this job keeps its DNA's beast type and only improves purities.")
                    .BindOnMouseUp(model => model.OnClickAllowMutationToggled());
            });

            col.AddRow(row =>
            {
                row.AddCheckBox()
                    .SetText(" Use Erratic Genius")
                    .BindIsChecked(model => model.IsErraticGeniusChecked)
                    .BindIsEnabled(model => model.IsErraticGeniusEnabled)
                    .BindTooltip(model => model.ErraticGeniusTooltip)
                    .BindOnMouseUp(model => model.OnClickErraticGeniusToggled());
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.RecipeHeader)
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.RecipeLines)
                                .BindColor(model => model.RecipeLineColors)
                                .BindTooltip(model => model.RecipeLines)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                    .BindRowCount(model => model.RecipeLines)
                    .SetRowHeight(22f);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.UnknownRecipesText)
                    .SetColor(GuiColor.Grey)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }
    }
}
