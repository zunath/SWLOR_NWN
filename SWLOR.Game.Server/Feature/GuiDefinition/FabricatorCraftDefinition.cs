using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Fabrication crafting window: the Fabricator minigame.</summary>
    public class FabricatorCraftDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 660f;
        private const float SlotRowHeight = 30f;
        private const float SlotListHeight = 4 * SlotRowHeight + 8f;
        private const float ActionRowHeight = 46f;
        private const float ActionListHeight = 9 * ActionRowHeight + 8f;
        private const float ActionButtonWidth = 170f;
        private const float PartLabelWidth = 250f;

        private static readonly string HelpText =
            "GOAL\n" +
            "Build the piece's three sub-assemblies, then Assemble it. The Frame is worked by the Cutter, the Shell by the Extruder and the Fittings by the Bonder. " +
            "Each has progress, which must be full, and quality. Machine Condition is the fabricator's health: if it reaches 0 the piece breaks and each component stack and " +
            $"each enhancement is lost on a roll of {CraftOutcomeRules.ChanceToLoseItemPercent} or lower on a D100. " +
            $"An enhancement transfers when the quality reaches its slot's threshold: {CraftOutcomeRules.FirstSlotQualityThresholdPercent}% for the first slot and " +
            $"{CraftOutcomeRules.SecondSlotQualityThresholdPercent}% for the others.\n\n" +
            "TOOL HEADS\n" +
            "One head is loaded at a time. Run builds a sub-assembly: at full effect when its own head is loaded, and at half effect when another head is loaded, with no retooling. " +
            "Precision Pass raises quality, and it only works on the sub-assembly the loaded head serves, so to refine all three you must retool. " +
            "Retool loads another head and costs CP. Every Run and Precision Pass wears the machine; this recipe's wear is shown above, and harder materials wear it faster.\n\n" +
            "QUALITY\n" +
            "The piece's quality is the average of the three sub-assemblies' quality, weighted by this recipe's quality shares (a bed is mostly Frame, a terminal mostly Fittings), " +
            $"and it can be no higher than the weakest sub-assembly plus {FabricatorRules.AssembleCapPoints}. The cap is shown on the quality meter. Lift your weakest sub-assembly to raise it.\n\n" +
            "ACTIONS\n" +
            "Run builds a sub-assembly. Precision Pass refines the loaded head's sub-assembly. Retool changes heads and costs CP. " +
            $"Service restores {FabricatorRules.ServiceRestore} Machine Condition and costs CP (Fabrication rank {FabricatorRules.ServiceRank}). " +
            "Assemble finishes the item once all three sub-assemblies are complete; its button says that it finishes the item and at what quality, so refine first.\n\n" +
            "PRIME FEEDSTOCK\n" +
            $"Sometimes a head is announced as primed: its next action gets +{FabricatorRules.PrimeBonusPercent}%. It waits for that head for {FabricatorRules.PrimeWindowActions} actions, " +
            "so you can retool to it in time. It only ever helps.\n\n" +
            "GEAR\n" +
            "Craftsmanship makes Run stronger and Control makes Precision Pass stronger. CP is your gear's CP plus a share of your Fabrication rank.";

        private readonly GuiWindowBuilder<FabricatorCraftViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CraftFabricator)
                .SetInitialGeometry(0, 0, 740f, 820f)
                .SetTitle("Fabricator")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(FabricatorCraftViewModel.SetupPartial, AddSetup)
                .DefinePartialView(FabricatorCraftViewModel.CraftPartial, AddCraft)
                .DefinePartialView(
                    CraftMinigameHelp.PartialName,
                    group => CraftMinigameHelpPartial.Build(group, "How the Fabricator Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(FabricatorCraftViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddSetup(GuiGroup<FabricatorCraftViewModel> host)
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
                            content.AddRow(row2 => row2.AddText()
                                .BindText(model => model.FabricatorSummary)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None)
                                .SetHeight(40f));
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

        private static void AddCraft(GuiGroup<FabricatorCraftViewModel> host)
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
                            AddLabelRow(content, model => model.HeadText);
                            content.AddRow(row2 => row2.AddLabel()
                                .BindText(model => model.OpportunityText)
                                .BindColor(model => model.OpportunityColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f));
                            AddPartRow(content, model => model.FrameText, model => model.FrameProgress, model => model.FrameQuality);
                            AddPartRow(content, model => model.ShellText, model => model.ShellProgress, model => model.ShellQuality);
                            AddPartRow(content, model => model.FittingsText, model => model.FittingsProgress, model => model.FittingsQuality);
                            AddMeter(content, model => model.QualityText, model => model.QualityFraction, 0, 191, 255);
                            AddMeter(content, model => model.MachineText, model => model.MachineFraction, 169, 169, 169);
                            AddLabelRow(content, model => model.CpText);
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

        private static void AddTitleRow(GuiColumn<FabricatorCraftViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.RecipeName)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetHeight(24f);
                CraftMinigameHelpPartial.AddHelpButton(row, "How the Fabricator works", model => model.OnHelp());
            });
        }

        private static void AddLabelRow(
            GuiColumn<FabricatorCraftViewModel> content,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, string>> text)
        {
            content.AddRow(row => row.AddLabel()
                .BindText(text)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));
        }

        /// <summary>One sub-assembly: its name and numbers, then its progress bar and its quality bar side by side.</summary>
        private static void AddPartRow(
            GuiColumn<FabricatorCraftViewModel> content,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, string>> text,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, float>> progress,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, float>> quality)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(text)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetWidth(PartLabelWidth)
                    .SetHeight(20f);
                row.AddProgressBar()
                    .BindValue(progress)
                    .SetColor(50, 205, 50);
                row.AddProgressBar()
                    .BindValue(quality)
                    .SetColor(0, 191, 255);
            });
        }

        private static void AddMeter(
            GuiColumn<FabricatorCraftViewModel> content,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, string>> text,
            System.Linq.Expressions.Expression<System.Func<FabricatorCraftViewModel, float>> value,
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
