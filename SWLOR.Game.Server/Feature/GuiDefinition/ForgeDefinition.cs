using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Plasma Forge: the window every Smithery recipe is crafted in.</summary>
    public class ForgeDefinition : IGuiWindowDefinition
    {
        private const float ContentPanelWidth = 600f;
        private const float ContentWidth = ContentPanelWidth - 24f;
        private const float ButtonWidth = 190f;
        private const float MeterBarHeight = 18f;
        private const float SlotIconSize = 36f;

        private const string HelpText =
            "THE GOAL\n" +
            "Shape the piece until the Shape meter is full, and finish it as well as you can before then. The craft ends the moment Shape " +
            "is full, so the Finish you have by then is the quality of the item. The action that would complete Shape says " +
            "FINISHES THE ITEM beside it.\n\n" +
            "THERMAL\n" +
            "Every metal works best inside its Working range on the Thermal meter. Thermal falls after every action by the recipe's " +
            "cooling rate. Leather and synthweave are heat-sealed: they use a low, wide range that cools slowly.\n\n" +
            "ACTIONS\n" +
            "- Ramp Plasma raises Thermal and costs CP.\n" +
            "- Hydro-Press gains a large amount of Shape, more the hotter the piece is inside the range. Below the range it does little and " +
            "cracks Integrity; above it, it shapes more but cracks Integrity harder.\n" +
            "- Vibro-Hone gains Finish. It is best in the cool half of the range and weak outside it.\n" +
            "- Rebond (rank 10) restores Integrity and costs Thermal and CP.\n" +
            "- Cryo-Quench (rank 20) works once per craft, after the piece is 80% shaped. It gives a large Finish bonus when Thermal is in the range, " +
            "more near its center. Afterward the piece is set: Ramp Plasma and Hydro-Press are disabled, and Vibro-Hone works at a flat rate " +
            "and trims the set piece to size.\n" +
            "Rebond unlocks at Smithery rank 10 and Cryo-Quench at rank 20; the rest are open from the start. The list shows the exact " +
            "result of every action before you click.\n\n" +
            "STABLE CONTAINMENT\n" +
            "Sometimes the forge announces Stable Containment: your next Hydro-Press or Vibro-Hone gets +50%. It lasts for one action and " +
            "never costs you anything.\n\n" +
            "YOUR GEAR\n" +
            "Craftsmanship raises Shape gains and Control raises Finish gains. Craft points are your gear's CP plus three quarters of " +
            "your Smithery rank.\n\n" +
            "ENHANCEMENTS AND FAILURE\n" +
            "Each enhancement slot has a Finish threshold: if your Finish meets it when the item is finished, the enhancement transfers " +
            "for certain. The piece breaks if Integrity reaches 0. Stopping early, closing the window or walking away also ends the " +
            "craft as a failure, and each component stack and each enhancement is then lost on a 65% chance.";

        private readonly GuiWindowBuilder<ForgeViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Forge)
                .SetInitialGeometry(0, 0, 680f, 760f)
                .SetTitle("Plasma Forge")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ForgeViewModel.SetupPartial, AddSetup)
                .DefinePartialView(ForgeViewModel.CraftPartial, AddCraft)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How the Plasma Forge Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(ForgeViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddSetup(GuiGroup<ForgeViewModel> host)
        {
            host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    AddHeader(content);

                    content.AddRow(row => row.AddText()
                        .BindText(model => model.FailureRuleText)
                        .SetWidth(ContentWidth)
                        .SetHeight(66f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));

                    content.AddRow(row => row.AddLabel()
                        .SetText("Enhancements")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f));

                    content.AddRow(row => row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(SlotIconSize + 8f);
                            cell.AddButtonImage()
                                .BindImageResref(model => model.SlotIcons)
                                .SetWidth(SlotIconSize)
                                .SetHeight(SlotIconSize)
                                .BindOnClicked(model => model.OnClickEnhancementSlot());
                        });
                        template.AddCell(cell => cell.AddLabel()
                            .BindText(model => model.SlotLabels)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle));
                    })
                        .BindRowCount(model => model.SlotLabels)
                        .SetWidth(ContentWidth)
                        .SetRowHeight(SlotIconSize + 8f)
                        .SetHeight(130f));

                    content.AddRow(row => row.AddLabel()
                        .BindText(model => model.EnhancementSummaryText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f));

                    content.AddRow(row => row.AddList(template =>
                    {
                        template.AddCell(cell => cell.AddLabel()
                            .BindText(model => model.RecipeDescription)
                            .BindColor(model => model.RecipeColors)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle));
                    })
                        .BindRowCount(model => model.RecipeDescription)
                        .SetWidth(ContentWidth)
                        .SetRowHeight(22f)
                        .SetHeight(210f));

                    content.AddRow(row => row.AddLabel()
                        .BindText(model => model.StatusText)
                        .BindColor(model => model.StatusColor)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetHeight(24f));

                    content.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindText(model => model.CraftButtonText)
                            .BindIsEnabled(model => model.IsCraftEnabled)
                            .SetWidth(240f)
                            .SetHeight(36f)
                            .BindOnClicked(model => model.OnClickCraft());
                        row.AddSpacer();
                    });
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddCraft(GuiGroup<ForgeViewModel> host)
        {
            host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    AddHeader(content);

                    content.AddRow(row => row.AddLabel()
                        .BindText(model => model.OpportunityText)
                        .BindColor(model => model.OpportunityTextColor)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(22f));

                    AddMeter(content, model => model.ThermalText, model => model.ThermalValue, model => model.ThermalColor);
                    AddMeter(content, model => model.ShapeText, model => model.ShapeValue, 50, 205, 50);
                    AddMeter(content, model => model.FinishText, model => model.FinishValue, 0, 191, 255);
                    content.AddRow(row => row.AddLabel()
                        .BindText(model => model.ThresholdsText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f));
                    AddMeter(content, model => model.IntegrityText, model => model.IntegrityValue, 169, 169, 169);
                    AddMeter(content, model => model.CpText, model => model.CpValue, 255, 215, 0);

                    content.AddRow(row => row.AddLabel()
                        .SetText("Actions")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f));

                    content.AddRow(row => row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(ButtonWidth);
                            cell.AddButton()
                                .BindText(model => model.ActionNames)
                                .BindIsEnabled(model => model.ActionEnabled)
                                .BindTooltip(model => model.ActionTooltips)
                                .SetHeight(34f)
                                .BindOnClicked(model => model.OnClickAction());
                        });
                        template.AddCell(cell => cell.AddLabel()
                            .BindText(model => model.ActionPreviews)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle));
                    })
                        .BindRowCount(model => model.ActionNames)
                        .SetWidth(ContentWidth)
                        .SetRowHeight(52f)
                        .SetHeight(270f));

                    content.AddRow(row => row.AddText()
                        .BindText(model => model.LastResultText)
                        .SetWidth(ContentWidth)
                        .SetHeight(44f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));

                    content.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Stop Crafting")
                            .SetTooltip("Ends the craft as a failure.")
                            .SetWidth(180f)
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickStop());
                        row.AddSpacer();
                    });
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddHeader(GuiColumn<ForgeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.RecipeName)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetHeight(24f);
                CraftMinigameHelpPartial.AddHelpButton(row, "How the Plasma Forge works", model => model.OnHelp());
            });

            content.AddRow(row => row.AddLabel()
                .BindText(model => model.RecipeLevelText)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));

            content.AddRow(row => row.AddLabel()
                .BindText(model => model.YourSkillText)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));
        }

        private static void AddMeter(
            GuiColumn<ForgeViewModel> content,
            System.Linq.Expressions.Expression<Func<ForgeViewModel, string>> text,
            System.Linq.Expressions.Expression<Func<ForgeViewModel, float>> value,
            byte red,
            byte green,
            byte blue)
        {
            content.AddRow(row => row.AddLabel()
                .BindText(text)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));
            content.AddRow(row => row.AddProgressBar()
                .BindValue(value)
                .SetColor(red, green, blue)
                .SetHeight(MeterBarHeight));
        }

        private static void AddMeter(
            GuiColumn<ForgeViewModel> content,
            System.Linq.Expressions.Expression<Func<ForgeViewModel, string>> text,
            System.Linq.Expressions.Expression<Func<ForgeViewModel, float>> value,
            System.Linq.Expressions.Expression<Func<ForgeViewModel, GuiColor>> color)
        {
            content.AddRow(row => row.AddLabel()
                .BindText(text)
                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                .SetHeight(20f));
            content.AddRow(row => row.AddProgressBar()
                .BindValue(value)
                .BindColor(color)
                .SetHeight(MeterBarHeight));
        }
    }
}
