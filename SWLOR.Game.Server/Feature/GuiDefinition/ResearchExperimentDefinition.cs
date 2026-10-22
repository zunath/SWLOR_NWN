using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Experiment window: the short run of trials every research level starts with.</summary>
    public class ResearchExperimentDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 580f;
        private const float ActionButtonWidth = 190f;
        private const float ActionButtonHeight = 32f;
        private const float MeterLabelHeight = 20f;
        private const float MeterBarHeight = 18f;

        private static readonly string HelpText =
            "WHAT IT IS\n" +
            "Every research level starts with a short experiment. The job's credits are spent when it starts, and the job then runs in " +
            "real time as before. The experiment can shorten the job, pick and size the blueprint bonus at levels 3, 5 and 8, and " +
            "earns Fabrication XP.\n\n" +
            "TRIALS\n" +
            $"You have {ExperimentRules.Trials} trials. A Test or an Analyze uses one. Refine Method and Conclude use none. The experiment " +
            "ends when the trials run out or you Conclude.\n\n" +
            "ACTIONS\n" +
            "- Test: gather evidence. Each button shows its chance of finding evidence. A Test that finds some adds 1 to its candidate.\n" +
            $"- Refine Method: costs {ExperimentRules.RefineCp} Fabrication CP and raises the chance of your next Test by " +
            $"{ExperimentRules.RefineChanceBonusPercent} points. That Test uses it up whether or not it finds evidence, and you cannot " +
            "stack it.\n" +
            $"- Analyze: spends your spare evidence to cut the job's time, {ExperimentRules.TimeCutPercentPerEvidence}% for each piece, " +
            $"up to {ExperimentRules.TimeCutCapPercent}% in all. It uses one trial, so keep one back for it.\n" +
            "- Conclude: end the experiment now with the evidence so far.\n\n" +
            "LEVELS 3, 5 AND 8\n" +
            $"These levels grant one of three bonuses: licensed runs (up to +{ResearchGrantRules.LicensedRunsMaximum}), credit reduction " +
            $"(up to {ResearchGrantRules.ReductionMaximum}%) or time reduction (up to {ResearchGrantRules.ReductionMaximum}%). Test the one " +
            "you want. The candidate with the most evidence wins, and more evidence pays more, never beyond those limits. A tie goes to " +
            "one of the tied candidates at random. Evidence behind the leader is spare and can be analyzed; the leader keeps its own. " +
            "If you finish with no evidence on any candidate, the bonus is rolled when the job completes, as it always was.\n\n" +
            "OTHER LEVELS\n" +
            "The level's grant is fixed. All your evidence is spare, so Analyze it for a shorter job.\n\n" +
            "EXTRA BONUS\n" +
            "Each level has a small chance of an extra guaranteed bonus. It is rolled once when the experiment opens, shown in this " +
            "window, and added when the job completes. Evidence does not change it.\n\n" +
            "LEAVING\n" +
            "Closing this window, walking away from the terminal or logging out ends the experiment with the evidence gathered so far. " +
            "The job keeps running either way, and a job with an experiment cannot be cancelled, so the result you see is the result you get.\n\n" +
            "XP\n" +
            "The experiment and the finished job give Fabrication XP, based on the recipe's level against your Fabrication rank. Research " +
            "far below your rank gives none.";

        private readonly GuiWindowBuilder<ResearchExperimentViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ResearchExperiment)
                .SetInitialGeometry(0, 0, 640f, 800f)
                .SetTitle("Research Experiment")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ResearchExperimentViewModel.MainPartial, AddMain)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How the Experiment Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(ResearchExperimentViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddMain(GuiGroup<ResearchExperimentViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(AddPanelContent);
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddPanelContent(GuiColumn<ResearchExperimentViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.RecipeName)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                CraftMinigameHelpPartial.AddHelpButton(row, "How the Experiment works", model => model.OnHelp());
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.LevelText)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StatusText)
                    .BindColor(model => model.StatusColor)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.ExtraBonusText)
                    .BindColor(model => model.ExtraBonusColor)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.JobTimeText)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            AddMeter(content, model => model.TrialsText, model => model.TrialsPercent, 200, 200, 200);

            for (var slot = 0; slot < ResearchExperimentViewModel.EvidenceSlotCount; slot++)
            {
                var index = slot;
                AddEvidence(content, index);
                content.AddRow(row =>
                {
                    AddActionRow(row, index);
                });
            }

            AddMeter(content, model => model.TimeCutText, model => model.TimeCutPercent, 255, 165, 0);

            for (var action = ResearchExperimentViewModel.EvidenceSlotCount; action < ResearchExperimentViewModel.ActionCount; action++)
            {
                var index = action;
                content.AddRow(row =>
                {
                    AddActionRow(row, index);
                });
            }

            AddMeter(content, model => model.CpText, model => model.CpPercent, 0, 138, 250);

            content.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetText("Close")
                    .SetTooltip("Close the experiment. If it is still going, it ends with the evidence so far.")
                    .SetWidth(140f)
                    .SetHeight(34f)
                    .BindOnClicked(model => model.OnClickClose());
                row.AddSpacer();
            });
        }

        private static void AddEvidence(GuiColumn<ResearchExperimentViewModel> content, int slot)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(Binding<string>(ResearchExperimentViewModel.EvidenceTextProperty(slot)))
                    .BindIsVisible(Binding<bool>(ResearchExperimentViewModel.EvidenceVisibleProperty(slot)))
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(Binding<float>(ResearchExperimentViewModel.EvidencePercentProperty(slot)))
                    .BindIsVisible(Binding<bool>(ResearchExperimentViewModel.EvidenceVisibleProperty(slot)))
                    .SetHeight(MeterBarHeight)
                    .SetColor(ResearchExperimentViewModel.EvidenceColor(slot));
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(Binding<string>(ResearchExperimentViewModel.EvidenceProjectionProperty(slot)))
                    .BindIsVisible(Binding<bool>(ResearchExperimentViewModel.EvidenceVisibleProperty(slot)))
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddActionRow(GuiRow<ResearchExperimentViewModel> row, int index)
        {
            row.AddButton()
                .BindText(Binding<string>(ResearchExperimentViewModel.ActionTextProperty(index)))
                .BindTooltip(Binding<string>(ResearchExperimentViewModel.ActionTooltipProperty(index)))
                .BindIsEnabled(Binding<bool>(ResearchExperimentViewModel.ActionEnabledProperty(index)))
                .BindIsVisible(Binding<bool>(ResearchExperimentViewModel.ActionVisibleProperty(index)))
                .BindOnClicked(model => model.OnClickAction(index))
                .SetWidth(ActionButtonWidth)
                .SetHeight(ActionButtonHeight);
            row.AddLabel()
                .BindText(Binding<string>(ResearchExperimentViewModel.ActionSummaryProperty(index)))
                .BindColor(Binding<GuiColor>(ResearchExperimentViewModel.ActionColorProperty(index)))
                .BindIsVisible(Binding<bool>(ResearchExperimentViewModel.ActionVisibleProperty(index)))
                .SetHeight(ActionButtonHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetVerticalAlign(NuiVerticalAlign.Middle);
        }

        private static void AddMeter(
            GuiColumn<ResearchExperimentViewModel> content,
            Expression<Func<ResearchExperimentViewModel, string>> text,
            Expression<Func<ResearchExperimentViewModel, float>> percent,
            byte red,
            byte green,
            byte blue)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(text)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(percent)
                    .SetHeight(MeterBarHeight)
                    .SetColor(red, green, blue);
            });
        }

        private static Expression<Func<ResearchExperimentViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ResearchExperimentViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<ResearchExperimentViewModel, TProperty>>(property, model);
        }
    }
}
