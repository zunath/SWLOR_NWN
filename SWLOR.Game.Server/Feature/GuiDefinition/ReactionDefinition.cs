using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Reaction window: Espionage's poison and trap crafting.</summary>
    public class ReactionDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 580f;
        private const float StatusTextHeight = 66f;
        private const float ActionButtonWidth = 130f;
        private const float ActionButtonHeight = 32f;
        private const float MeterLabelHeight = 20f;
        private const float MeterBarHeight = 18f;
        private const float RecipeDetailHeight = 210f;
        private const float RuleTextHeight = 52f;

        private static readonly int FailureXpPercent = (int)(CraftOutcomeRules.FailureXpFraction * 100);

        private static readonly string HelpText =
            "THE CYCLE\n" +
            "You work a volatile poison or trap mechanism through a reaction cycle. It runs Calm, Rising, Peak and Settling, and it " +
            "advances by one turn after every action. The window always shows the current phase and the next two, and the whole cycle " +
            "with your position in brackets. The reaction is already under way when you begin, so you may start in any phase.\n\n" +
            "METERS\n" +
            "Potency is your progress: fill it to finish the item. Subtlety is quality. It sets your XP bonus and the sale value, and " +
            "it is locked in the moment Potency fills, so build it first. Stability is the strength of the piece: if it reaches 0, the " +
            "piece breaks. CP pays for Dampen and Stabilize.\n\n" +
            "ACTIONS\n" +
            "- Measure: a small amount of Potency. Safe in any phase.\n" +
            "- Charge: a large amount of Potency, doubled at Peak. It costs heavy Stability unless the phase is Calm. The Peak is " +
            "harsher in higher-tier recipes.\n" +
            "- Refine: Subtlety, doubled in Settling and halved at Peak.\n" +
            "- Dampen: holds the current phase for one more turn and restores a little Stability. It does not wear the piece. " +
            "It costs 3 CP for every 20 recipe levels (at least 2), so before you learn Stabilize it is your only repair, and " +
            "afterwards a crafter at the recipe's level has one to spare beside a Stabilize.\n" +
            "- Stabilize: costs CP and restores Stability. You learn it at Espionage rank 10, and it costs 3 CP for every 5 recipe " +
            "levels, so a crafter at the recipe's level can afford it once.\n" +
            "Measure, Charge and Refine also cost some Stability, and more in the higher tiers. Each button shows exactly what it will do before " +
            "you click, including the phase that follows. The action that would finish the item says so.\n\n" +
            "FAILING\n" +
            "If Stability reaches 0, or you close this window, die or walk away from the bench, the piece is lost. Each stack of " +
            $"components then has a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance to be lost, and you still earn {FailureXpPercent}% of the base XP.\n\n" +
            "XP\n" +
            "Rushing through the actions does not earn more XP than the normal pace.\n\n" +
            "YOUR RANK\n" +
            "Espionage rank sets how strong your actions are and how much CP you have. Poisoncraft, Trapcraft and Master Saboteur still " +
            "decide which recipes you can craft.";

        private static readonly string FailureRuleText =
            $"If the piece breaks, or you close this window, die or leave the bench, each stack of components has a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance " +
            $"to be lost, and the attempt still earns {FailureXpPercent}% of the base XP.";

        private readonly GuiWindowBuilder<ReactionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Reaction)
                .SetInitialGeometry(0, 0, 640f, 660f)
                .SetTitle("Reaction Bench")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ReactionViewModel.SetupPartial, AddSetup)
                .DefinePartialView(ReactionViewModel.CraftPartial, AddCraft)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How the Reaction Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(ReactionViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddSetup(GuiGroup<ReactionViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.RecipeName)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                    CraftMinigameHelpPartial.AddHelpButton(row, "How the Reaction works", model => model.OnHelp());
                });
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.RecipeLevelText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.SkillText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                content.AddRow(row =>
                {
                    row.SetHeight(RecipeDetailHeight);
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.RecipeDescription)
                                .BindColor(model => model.RecipeColors)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });
                    })
                        .BindRowCount(model => model.RecipeDescription);
                });
                content.AddRow(row =>
                {
                    row.SetHeight(RuleTextHeight);
                    row.AddText()
                        .SetText(FailureRuleText)
                        .SetShowBorder(false);
                });
                content.AddRow(row =>
                {
                    // Wrapped text, not a one-line label: the result sentences run past one line.
                    row.AddText()
                        .BindText(model => model.StatusText)
                        .BindColor(model => model.StatusColor)
                        .SetHeight(StatusTextHeight)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None);
                });
                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .BindText(model => model.BeginText)
                        .SetTooltip("Commit the components and start the reaction.")
                        .SetWidth(200f)
                        .SetHeight(36f)
                        .BindOnClicked(model => model.OnClickBegin());
                    row.AddSpacer();
                });
            });
        }

        private static void AddCraft(GuiGroup<ReactionViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.RecipeName)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                    CraftMinigameHelpPartial.AddHelpButton(row, "How the Reaction works", model => model.OnHelp());
                });
                content.AddRow(row =>
                {
                    // Wrapped text, not a one-line label: the result sentences run past one line.
                    row.AddText()
                        .BindText(model => model.StatusText)
                        .BindColor(model => model.StatusColor)
                        .SetHeight(StatusTextHeight)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None);
                });

                content.AddRow(row =>
                {
                    AddPhaseLabel(row, model => model.PhaseNowText, model => model.PhaseNowColor);
                    AddPhaseLabel(row, model => model.PhaseNextText, model => model.PhaseNextColor);
                    AddPhaseLabel(row, model => model.PhaseThenText, model => model.PhaseThenColor);
                });
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.PhaseEffectText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.CycleText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.PeakText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                AddMeter(content, model => model.PotencyText, model => model.PotencyPercent, 50, 205, 50);
                AddMeter(content, model => model.SubtletyText, model => model.SubtletyPercent, 0, 191, 255);
                AddMeter(content, model => model.StabilityText, model => model.StabilityPercent, 255, 165, 0);
                AddMeter(content, model => model.CpText, model => model.CpPercent, 0, 138, 250);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Actions")
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                for (var action = 0; action < ReactionViewModel.ActionCount; action++)
                {
                    var index = action;
                    content.AddRow(row =>
                    {
                        row.AddButton()
                            .BindText(Binding<string>(ReactionViewModel.ActionTextProperty(index)))
                            .BindTooltip(Binding<string>(ReactionViewModel.ActionTooltipProperty(index)))
                            .BindIsEnabled(Binding<bool>(ReactionViewModel.ActionEnabledProperty(index)))
                            .BindOnClicked(model => model.OnClickAction(index))
                            .SetWidth(ActionButtonWidth)
                            .SetHeight(ActionButtonHeight);
                        row.AddLabel()
                            .BindText(Binding<string>(ReactionViewModel.ActionSummaryProperty(index)))
                            .BindColor(Binding<GuiColor>(ReactionViewModel.ActionColorProperty(index)))
                            .SetHeight(ActionButtonHeight)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });
                }

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Abort")
                        .SetTooltip($"Stop the reaction. Each stack of components has a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance to be lost.")
                        .SetWidth(140f)
                        .SetHeight(34f)
                        .BindOnClicked(model => model.OnClickAbort());
                    row.AddSpacer();
                });
            });
        }

        private static void AddPhaseLabel(
            GuiRow<ReactionViewModel> row,
            Expression<Func<ReactionViewModel, string>> text,
            Expression<Func<ReactionViewModel, GuiColor>> color)
        {
            row.AddLabel()
                .BindText(text)
                .BindColor(color)
                .SetHeight(28f)
                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                .SetVerticalAlign(NuiVerticalAlign.Middle);
        }

        private static void AddMeter(
            GuiColumn<ReactionViewModel> content,
            Expression<Func<ReactionViewModel, string>> text,
            Expression<Func<ReactionViewModel, float>> percent,
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

        /// <summary>A fixed-width borderless panel in the standard layout's content host (rule R5).</summary>
        private static void AddPanel(GuiGroup<ReactionViewModel> host, Action<GuiColumn<ReactionViewModel>> buildContent)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        // Auto: the host group only hands this panel the viewport height, so the panel is what scrolls on a short screen.
                        panel.SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(buildContent);
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static Expression<Func<ReactionViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ReactionViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<ReactionViewModel, TProperty>>(property, model);
        }
    }
}
