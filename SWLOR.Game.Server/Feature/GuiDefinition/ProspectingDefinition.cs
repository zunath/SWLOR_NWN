using System.Globalization;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Prospecting window: the node session a harvester opens on a vein or a tree.</summary>
    public class ProspectingDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 560f;
        private const float ActionButtonWidth = 130f;
        private const float ActionButtonHeight = 32f;
        private const float MeterLabelHeight = 20f;
        private const float MeterBarHeight = 18f;
        private const float SeamRowHeight = 20f;
        private const float StyleRowHeight = 40f;
        private const float StyleListHeight = 3 * StyleRowHeight + 8f;

        private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        private static readonly string HelpText =
            "THE NODE\n" +
            "A vein or tree holds a number of units of ore or wood, which is Yield, and a Stability of " + ProspectingRules.MaxStability + ". " +
            "When Stability reaches 0 the node collapses and what was left in it is lost. One to three seams are sealed inside it, " +
            "hidden until you find them. Each seam locks up one unit of the Yield.\n\n" +
            "ACTIONS\n" +
            "Each action spends Stability, takes a few seconds, and shows exactly what it will do before you click. Every action but Survey " +
            "also spends harvester charges.\n" +
            "- Survey: reveals hidden seams. It costs no charge, only Stability. It finds nothing once every seam has been found.\n" +
            "- Strike: takes 1 or 2 units from the Yield. It takes the loose units first and only breaks into a seam when none is left, " +
            "so striking a node empty always gets you every unit.\n" +
            "- Heavy Strike: takes 2 to 4 units for more charges and Stability. It has a " + ProspectingRules.ShatterPercent + "% chance to " +
            "shatter a gem pocket you have not surveyed, which turns its gem into plain ore.\n" +
            "- Extract: opens a seam you have found, for " + ProspectingRules.ExtractCharges + " charges. A rich seam gives its unit and has a " +
            ProspectingRules.RichSeamBonusPercent + "% chance of one extra ore roll. A gem pocket gives a gem in place of its unit.\n" +
            "- Leave: stops, and leaves the rest of the node for the next harvester. The seams you found stay found.\n\n" +
            "ENDING\n" +
            "Taking the last unit spends the node, and any seam still in it goes with it, so open the seams before you take the last " +
            "units. Everything you gathered is kept however the session ends. Closing this window, moving away, resting, entering combat " +
            "or dying also ends it, and so does leaving the window untouched for " + (int)ProspectingLifecycle.IdleSeconds + " seconds.\n\n" +
            "QUICK HARVEST\n" +
            "Quick Harvest takes everything the node has left with no seams, one unit and one charge every " +
            Number(ProspectingRules.HarvestSeconds) + " seconds, the way harvesting always worked. It is offered only before you begin " +
            "working the node.\n\n" +
            "ONE AT A TIME\n" +
            "Only one harvester works a node at a time. Anyone else is told it is in use until you finish.\n\n" +
            "PERKS AND STYLES\n" +
            "The Prospector and Treasure Seeker perk lines never touch Quick Harvest. Efficient Strikes, Rich Veins and Mother Lode make actions " +
            "quicker, Core Sample shows what is hidden in a node, and Sharp Chisel and Deep Scan look after gem pockets. At Gathering " +
            GatheringStyles.StyleRank + " the Strip Mining style makes Strikes quicker for more Stability, but Surveys and Extracts take longer; it " +
            "suits quick work that leaves the seams alone. Pick a style in the list below before your first action. Only one style can be active at a " +
            "time, and it stays until you pick another.\n\n" +
            "YOUR RANK\n" +
            "Your Gathering rank replaces your attributes here. At Gathering ranks " + string.Join(", ", CraftRankMilestones.ProspectingYieldMilestoneRanks) +
            " a Strike and a Heavy Strike take more units, and at ranks " + string.Join(" and ", CraftRankMilestones.ProspectingSurveyMilestoneRanks) +
            " a Survey reveals one more seam.\n\n" +
            "XP\n" +
            "Every item you gather pays Gathering XP by the level of the vein against your rank, as before.";

        private readonly GuiWindowBuilder<ProspectingViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Prospecting)
                .SetInitialGeometry(0, 0, 620f, 900f)
                .SetTitle("Prospecting")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ProspectingViewModel.NodePartial, AddNode)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How Prospecting Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(ProspectingViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddNode(GuiGroup<ProspectingViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(AddContent);
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddContent(GuiColumn<ProspectingViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.HeaderText)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                CraftMinigameHelpPartial.AddHelpButton(row, "How Prospecting works", model => model.OnHelp());
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StatusText)
                    .BindColor(model => model.StatusColor)
                    .SetHeight(40f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });

            AddMeter(content, model => model.YieldText, model => model.YieldPercent, 70, 130, 230);
            AddMeter(content, model => model.StabilityText, model => model.StabilityPercent, 255, 140, 0);
            AddMeter(content, model => model.ChargesText, model => model.ChargesPercent, 50, 205, 50);

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.GatheredText)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            for (var seam = 0; seam < ProspectingViewModel.SeamRows; seam++)
            {
                var index = seam;
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(Binding<string>(ProspectingViewModel.SeamTextProperty(index)))
                        .BindColor(Binding<GuiColor>(ProspectingViewModel.SeamColorProperty(index)))
                        .SetHeight(SeamRowHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
            }

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.CoreSampleText)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StyleHeader)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row => row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddText()
                        .BindText(model => model.StyleLabels)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None));
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(90f);
                        cell.SetIsVariable(false);
                        cell.AddButton()
                            .BindText(model => model.StyleButtonTexts)
                            .BindIsEnabled(model => model.StyleButtonEnabled)
                            .BindOnClicked(model => model.OnClickStyle());
                    });
                })
                .BindRowCount(model => model.StyleLabels)
                .SetRowHeight(StyleRowHeight)
                .SetHeight(StyleListHeight));

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Actions")
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            for (var action = 0; action < ProspectingViewModel.ActionCount; action++)
            {
                var index = action;
                content.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(Binding<string>(ProspectingViewModel.ActionTextProperty(index)))
                        .BindTooltip(Binding<string>(ProspectingViewModel.ActionTooltipProperty(index)))
                        .BindIsEnabled(Binding<bool>(ProspectingViewModel.ActionEnabledProperty(index)))
                        .BindOnClicked(model => model.OnClickAction(index))
                        .SetWidth(ActionButtonWidth)
                        .SetHeight(ActionButtonHeight);
                    row.AddLabel()
                        .BindText(Binding<string>(ProspectingViewModel.ActionSummaryProperty(index)))
                        .BindColor(Binding<GuiColor>(ProspectingViewModel.ActionColorProperty(index)))
                        .SetHeight(ActionButtonHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                });
            }

            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Quick Harvest")
                    .BindTooltip(model => model.QuickHarvestTooltip)
                    .BindIsEnabled(model => model.IsQuickHarvestEnabled)
                    .BindOnClicked(model => model.OnClickQuickHarvest())
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ActionButtonHeight);
                row.AddLabel()
                    .BindText(model => model.QuickHarvestText)
                    .SetHeight(ActionButtonHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
            });

            content.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .BindText(model => model.CloseText)
                    .SetTooltip("Closing the window ends the session. Everything you gathered is kept and the rest of the node stays.")
                    .SetWidth(180f)
                    .SetHeight(34f)
                    .BindOnClicked(model => model.OnClickClose());
                row.AddSpacer();
            });
        }

        private static void AddMeter(
            GuiColumn<ProspectingViewModel> content,
            Expression<Func<ProspectingViewModel, string>> text,
            Expression<Func<ProspectingViewModel, float>> percent,
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

        private static Expression<Func<ProspectingViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ProspectingViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<ProspectingViewModel, TProperty>>(property, model);
        }
    }
}
