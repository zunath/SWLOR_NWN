using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Engineering crafting window: the Circuit minigame.</summary>
    public class CircuitCraftDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 660f;
        private const float SlotRowHeight = 30f;
        private const float SlotListHeight = 4 * SlotRowHeight + 8f;
        private const float ActionRowHeight = 50f;
        private const float ActionListHeight = 7 * ActionRowHeight + 8f;
        private const float ActionButtonWidth = 170f;

        public static readonly string HelpText =
            "GOAL\n" +
            "Fill Assembly to finish the item. Calibration is the item's quality. Integrity is the piece's health: if it reaches 0 the " +
            $"piece breaks and each component stack and each enhancement is lost on a roll of {CraftOutcomeRules.ChanceToLoseItemPercent} or lower on a D100. " +
            $"An enhancement transfers when the quality reaches its slot's threshold: {ThresholdSummary()}. " +
            "The action that would fill Assembly says it finishes the item, so raise Calibration first.\n\n" +
            "LOAD\n" +
            $"Wire adds {CircuitRules.WireLoad} Load, Tune adds {CircuitRules.TuneLoad} and Boost adds {CircuitRules.BoostLoad}. Every Load point makes Wire, Tune and Discharge stronger; this recipe's percentage is shown above. " +
            "Load is marked with an overload line. An action that leaves Load above the line can overload: the action still happens, then you lose a large share of " +
            $"Integrity and Load drops to 0. The chance is {CircuitRules.OverloadChancePerPointPercent}% for every point above the line and is shown beside each action. " +
            "Better component grades raise the line.\n\n" +
            "ACTIONS\n" +
            "Wire builds Assembly and costs no CP.\n" +
            "Tune builds Calibration and costs CP.\n" +
            "Discharge spends all your Load as one burst into Assembly or Calibration. The more Load, the bigger the burst. It never overloads.\n" +
            $"Patch restores {CircuitRules.PatchRestore} Integrity and costs CP. It unlocks at Engineering rank {CircuitRules.PatchRank}.\n" +
            $"Ground lowers Load by {CircuitRules.GroundLoad}, costs CP and never overloads.\n" +
            $"Boost adds {CircuitRules.BoostLoad} Load and costs CP.\n\n" +
            "CLEAN SIGNAL\n" +
            "Sometimes the next action is announced as a Clean Signal: your next Tune is doubled. It lasts for that one action.\n\n" +
            "GEAR\n" +
            "Craftsmanship makes Assembly work stronger and Control makes Calibration work stronger. CP is your gear's CP plus a share of your Engineering rank.";

        private readonly GuiWindowBuilder<CircuitCraftViewModel> _builder = new();

        /// <summary>The slot thresholds as the shared outcome rules set them, such as "50% for the first slot, 85% for the second and 85% for the third and any later slot".</summary>
        private static string ThresholdSummary()
        {
            var thresholds = CraftOutcomeRules.GetEnhancementThresholds(3);
            return $"{thresholds[0]}% for the first slot, {thresholds[1]}% for the second and {thresholds[2]}% for the third and any later slot";
        }

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CraftCircuit)
                .SetInitialGeometry(0, 0, 740f, 760f)
                .SetTitle("Circuit Assembly")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(CircuitCraftViewModel.SetupPartial, AddSetup)
                .DefinePartialView(CircuitCraftViewModel.CraftPartial, AddCraft)
                .DefinePartialView(
                    CraftMinigameHelp.PartialName,
                    group => CraftMinigameHelpPartial.Build(group, "How the Circuit Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(CircuitCraftViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddSetup(GuiGroup<CircuitCraftViewModel> host)
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
                            AddTitleRow(content);
                            AddLabelRow(content, model => model.RecipeLevel);
                            AddLabelRow(content, model => model.GearSummary);
                            AddLabelRow(content, model => model.CircuitSummary);
                            content.AddRow(row2 => row2.AddText()
                                .BindText(model => model.NeedsSummary)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None)
                                .SetHeight(40f));
                            AddLabelRow(content, model => model.SlotHeader);
                            content.AddRow(row2 => row2.AddList(template =>
                                {
                                    template.AddCell(cell => cell.AddLabel()
                                        .BindText(model => model.SlotLabels)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(90f);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .BindText(model => model.SlotButtonTexts)
                                            .BindOnClicked(model => model.OnClickSlot());
                                    });
                                })
                                .BindRowCount(model => model.SlotLabels)
                                .SetRowHeight(SlotRowHeight)
                                .SetHeight(SlotListHeight));
                            content.AddRow(row2 => row2.AddText()
                                .BindText(model => model.RuleText)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None)
                                .SetHeight(56f));
                            content.AddRow(row2 => row2.AddList(template =>
                                    template.AddCell(cell => cell.AddLabel()
                                        .BindText(model => model.RecipeDescription)
                                        .BindColor(model => model.RecipeColors)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetVerticalAlign(NuiVerticalAlign.Middle)))
                                .BindRowCount(model => model.RecipeDescription));
                            content.AddRow(row2 => row2.AddLabel()
                                .BindText(model => model.StatusText)
                                .BindColor(model => model.StatusColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetHeight(22f));
                            content.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .BindText(model => model.CraftText)
                                    .BindOnClicked(model => model.OnClickCraft())
                                    .SetWidth(200f)
                                    .SetHeight(35f);
                                row2.AddSpacer();
                            });
                        });
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddCraft(GuiGroup<CircuitCraftViewModel> host)
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
                            AddTitleRow(content);
                            content.AddRow(row2 => row2.AddLabel()
                                .BindText(model => model.StatusText)
                                .BindColor(model => model.StatusColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetHeight(22f));
                            AddMeter(content, model => model.AssemblyText, model => model.AssemblyFraction, 50, 205, 50);
                            AddMeter(content, model => model.CalibrationText, model => model.CalibrationFraction, 0, 191, 255);
                            AddMeter(content, model => model.IntegrityText, model => model.IntegrityFraction, 169, 169, 169);
                            content.AddRow(row2 => row2.AddLabel()
                                .BindText(model => model.LoadText)
                                .BindColor(model => model.LoadColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f));
                            content.AddRow(row2 => row2.AddProgressBar()
                                .BindValue(model => model.LoadFraction)
                                .SetColor(255, 165, 0));
                            content.AddRow(row2 => row2.AddText()
                                .BindText(model => model.LoadRuleText)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None)
                                .SetHeight(48f));
                            AddLabelRow(content, model => model.CpText);
                            content.AddRow(row2 => row2.AddLabel()
                                .BindText(model => model.OpportunityText)
                                .BindColor(model => model.OpportunityColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f));
                            AddLabelRow(content, model => model.ThresholdText);
                            content.AddRow(row2 => row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(ActionButtonWidth);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .BindText(model => model.ActionNames)
                                            .BindTooltip(model => model.ActionTooltips)
                                            .BindIsEnabled(model => model.ActionEnabled)
                                            .BindOnClicked(model => model.OnClickAction());
                                    });
                                    template.AddCell(cell => cell.AddText()
                                        .BindText(model => model.ActionTexts)
                                        .SetShowBorder(false)
                                        .SetScrollbars(NuiScrollbars.None));
                                })
                                .BindRowCount(model => model.ActionNames)
                                .SetRowHeight(ActionRowHeight)
                                .SetHeight(ActionListHeight));
                        });
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddTitleRow(GuiColumn<CircuitCraftViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.RecipeName)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetHeight(24f);
                CraftMinigameHelpPartial.AddHelpButton(row, "How the Circuit works", model => model.OnHelp());
            });
        }

        private static void AddLabelRow(
            GuiColumn<CircuitCraftViewModel> content,
            System.Linq.Expressions.Expression<System.Func<CircuitCraftViewModel, string>> text)
        {
            content.AddRow(row => row.AddLabel()
                .BindText(text)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));
        }

        private static void AddMeter(
            GuiColumn<CircuitCraftViewModel> content,
            System.Linq.Expressions.Expression<System.Func<CircuitCraftViewModel, string>> text,
            System.Linq.Expressions.Expression<System.Func<CircuitCraftViewModel, float>> value,
            byte red,
            byte green,
            byte blue)
        {
            AddLabelRow(content, text);
            content.AddRow(row => row.AddProgressBar()
                .BindValue(value)
                .SetColor(red, green, blue));
        }
    }
}
