using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Kitchen window: every Agriculture recipe is cooked here.</summary>
    public class KitchenDefinition : IGuiWindowDefinition
    {
        public const string HelpText =
            "GOAL\n" +
            "Steer the dish's flavor into the recipe's target zone while it cooks, then finish cooking it. " +
            "Doneness is your progress: the dish is finished when it reaches the top. Freshness falls every turn, and the dish " +
            "is ruined if it reaches 0.\n\n" +
            "FLAVOR AND QUALITY\n" +
            "The dish has three flavors: Savory, Sweet and Spice. The recipe has a target. Quality is 100% while the dish is " +
            "within the perfect distance of the target, and falls to 0% at the outer distance. The meters show where each " +
            "flavor stands against the target.\n\n" +
            "ADDING\n" +
            "Add puts a portion of a committed ingredient in the pot. Ingredients push hard but run out. The pantry (Salt, Sugar, " +
            "Spice) is free and unlimited, but each pinch is small. Every push lands light, steady or heavy, and every action " +
            "shows its exact range. Control narrows the range. Water thins every flavor.\n\n" +
            "HEAT\n" +
            "Adjust Heat takes a turn. Low heat cooks slowly and holds the flavor steady. Medium cooks at a steady pace and lets " +
            "the flavor drift a little. High cooks fast and drifts the flavor every turn. Simmer works on Low and Medium; Sear " +
            "works on High, gives a lot of Doneness and costs Freshness. The next drift is announced a turn ahead. Stir cancels " +
            "it for CP, and Cover halves Freshness loss for 3 turns for CP.\n\n" +
            "PEAK AROMA\n" +
            "Now and then Peak Aroma is announced for the next turn: your next Add is twice as strong.\n\n" +
            "ENHANCEMENTS AND FAILURE\n" +
            "Each enhancement slot shows the quality that guarantees its transfer. Before you start, the window shows what a " +
            "ruined dish costs. Closing the window ruins the dish too. Craftsmanship makes cooking faster, and Control narrows " +
            "your pushes.";

        private const float ContentPanelWidth = 620f;
        private const float LabelHeight = 20f;
        private const float BarHeight = 20f;
        private const float RowHeight = 34f;
        private const float NoteHeight = 44f;
        private const float ButtonWidth = 190f;
        private const float EnhancementButtonWidth = 90f;
        private const float AxisNameWidth = 70f;
        private const float AxisBarWidth = 130f;
        private const float StartButtonWidth = 220f;
        private const float StartButtonHeight = 35f;

        private readonly GuiWindowBuilder<KitchenViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Kitchen)
                .SetInitialGeometry(0f, 0f, 700f, 680f)
                .SetTitle("Kitchen")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(KitchenViewModel.SetupPartial, AddSetup)
                .DefinePartialView(KitchenViewModel.CookPartial, AddCook)
                .DefinePartialView(
                    CraftMinigameHelp.PartialName,
                    group => CraftMinigameHelpPartial.Build(group, "How the Kitchen Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(KitchenViewModel.ContentElement);
            });

            return _builder.Build();
        }

        private static void AddSetup(GuiGroup<KitchenViewModel> host)
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
                            content.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.RecipeName)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                CraftMinigameHelpPartial.AddHelpButton(r, "How the Kitchen works", model => model.OnHelp());
                            });
                            AddLabel(content, model => model.RecipeLevel);
                            AddNote(content, model => model.YourSkill);
                            AddNote(content, model => model.TargetText);

                            content.AddRow(r =>
                            {
                                r.AddList(template =>
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
                                    .BindRowCount(model => model.RecipeDescription)
                                    .SetRowHeight(22f)
                                    .SetHeight(230f);
                            });

                            AddLabel(content, null, "Enhancements (optional)");
                            content.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(EnhancementButtonWidth);
                                        cell.AddButton()
                                            .BindText(model => model.EnhancementButtons)
                                            .SetWidth(EnhancementButtonWidth)
                                            .BindOnClicked(model => model.OnClickEnhancement());
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.EnhancementTexts)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                })
                                    .BindRowCount(model => model.EnhancementTexts)
                                    .SetRowHeight(RowHeight)
                                    .SetHeight(106f);
                            });

                            AddNote(content, model => model.FailureRuleText);

                            content.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.StatusText)
                                    .BindColor(model => model.StatusColor)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
                            });

                            content.AddRow(r =>
                            {
                                r.AddSpacer();
                                r.AddButton()
                                    .BindText(model => model.StartButtonText)
                                    .BindIsEnabled(model => model.IsStartEnabled)
                                    .SetWidth(StartButtonWidth)
                                    .SetHeight(StartButtonHeight)
                                    .BindOnClicked(model => model.OnClickStart());
                                r.AddSpacer();
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddCook(GuiGroup<KitchenViewModel> host)
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
                            content.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.StatusText)
                                    .BindColor(model => model.StatusColor)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                CraftMinigameHelpPartial.AddHelpButton(r, "How the Kitchen works", model => model.OnHelp());
                            });

                            AddNote(content, model => model.LastResultText);
                            AddMeter(content, model => model.DonenessText, model => model.DonenessPercentage, 255, 140, 0);
                            AddMeter(content, model => model.FreshnessText, model => model.FreshnessPercentage, 0, 170, 0);
                            AddMeter(content, model => model.QualityText, model => model.QualityPercentage, 0, 138, 250);
                            AddLabel(content, model => model.CpText);

                            AddLabel(content, null, "Flavor");
                            content.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(AxisNameWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.AxisNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(AxisBarWidth);
                                        cell.AddProgressBar()
                                            .BindValue(model => model.AxisValues)
                                            .SetColor(220, 190, 60)
                                            .SetWidth(AxisBarWidth);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.AxisTexts)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                })
                                    .BindRowCount(model => model.AxisNames)
                                    .SetRowHeight(28f)
                                    .SetHeight(90f);
                            });

                            content.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.DistanceText)
                                    .BindColor(model => model.DistanceColor)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                            AddNote(content, model => model.HeatText);
                            AddNote(content, model => model.DriftText);
                            content.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.AromaText)
                                    .BindColor(model => model.AromaColor)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                            AddLabel(content, model => model.ThresholdText);

                            AddLabel(content, null, "Actions (hover for every result)");
                            content.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(ButtonWidth);
                                        cell.AddButton()
                                            .BindText(model => model.ActionNames)
                                            .BindTooltip(model => model.ActionTooltips)
                                            .BindIsEnabled(model => model.ActionEnabled)
                                            .SetWidth(ButtonWidth)
                                            .BindOnClicked(model => model.OnClickAction());
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.ActionSummaries)
                                            .BindColor(model => model.ActionColors)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                })
                                    .BindRowCount(model => model.ActionNames)
                                    .SetRowHeight(RowHeight)
                                    .SetHeight(440f);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        /// <summary>A one-line label. Pass a bound text, or null and a fixed text.</summary>
        private static void AddLabel(
            GuiColumn<KitchenViewModel> content,
            Expression<Func<KitchenViewModel, string>> text,
            string fixedText = null)
        {
            content.AddRow(r =>
            {
                var label = r.AddLabel()
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                if (text != null)
                    label.BindText(text);
                else
                    label.SetText(fixedText);
            });
        }

        /// <summary>Text that may wrap to a few lines.</summary>
        private static void AddNote(GuiColumn<KitchenViewModel> content, Expression<Func<KitchenViewModel, string>> text)
        {
            content.AddRow(r => r.AddText()
                .BindText(text)
                .SetShowBorder(false)
                .SetHeight(NoteHeight));
        }

        private static void AddMeter(
            GuiColumn<KitchenViewModel> content,
            Expression<Func<KitchenViewModel, string>> text,
            Expression<Func<KitchenViewModel, float>> value,
            byte red,
            byte green,
            byte blue)
        {
            AddLabel(content, text);
            content.AddRow(r => r.AddProgressBar()
                .BindValue(value)
                .SetColor(red, green, blue)
                .SetHeight(BarHeight));
        }
    }
}
