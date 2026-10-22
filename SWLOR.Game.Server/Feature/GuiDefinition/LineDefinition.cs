using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Line window: the fight a fishing bite starts.</summary>
    public class LineDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 560f;
        private const float ActionButtonWidth = 130f;
        private const float ActionButtonHeight = 32f;
        private const float MeterLabelHeight = 20f;
        private const float MeterBarHeight = 18f;
        private const float MoveHintHeight = 48f;

        private const string HelpText =
            "THE FIGHT\n" +
            "A bite hooks a fish. You win by reeling it in before it escapes or the line snaps. Three numbers tell you how it " +
            "stands: Distance (0 lands the fish, and at its maximum the fish escapes), Tension (at its maximum the line snaps) " +
            "and the fish's Stamina.\n\n" +
            "THE FISH'S MOVES\n" +
            "Before every turn the fish announces its move, and each action answers it differently:\n" +
            "- Run: the fish bolts. It takes line unless you reel against it, and reeling then strains the line badly.\n" +
            "- Dive: the fish plunges and the line goes taut whatever you do. Give Line absorbs half of it.\n" +
            "- Thrash: the fish thrashes and the Tension spikes. Only Hold shrugs it off.\n" +
            "- Tire: the fish rests and loses Stamina. Reeling pulls it in further.\n" +
            "A fish with no Stamina left only tires, so wearing it down makes the rest easy.\n\n" +
            "ACTIONS\n" +
            "- Set Hook (first turn only): drains the fish's Stamina. The fish's move is spent on the strike.\n" +
            "- Reel: closes Distance and adds Tension.\n" +
            "- Give Line: lowers Tension and lets out Distance.\n" +
            "- Hold: drains the fish's Stamina. Strong against a Thrash.\n" +
            "Each button shows exactly what it will do against the move the fish announced.\n\n" +
            "LOSING THE FISH\n" +
            "The bait, one of the fishing point's bites and your Agriculture XP are spent when the fish bites, whether you land it " +
            "or not. Closing this window, moving away, entering combat or dying lets the fish go.\n\n" +
            "YOUR GEAR\n" +
            "Your rod and your Agriculture rank set how much Tension the line takes and how far one Reel pulls the fish. A fish " +
            "above your rank is stronger and rests less, so it is harder to land.";

        private readonly GuiWindowBuilder<LineViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Line)
                .SetInitialGeometry(0, 0, 620f, 640f)
                .SetTitle("On the Line")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(LineViewModel.FightPartial, AddFight)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How the Line Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(LineViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddFight(GuiGroup<LineViewModel> host)
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

        private static void AddContent(GuiColumn<LineViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.HeaderText)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                CraftMinigameHelpPartial.AddHelpButton(row, "How the Line works", model => model.OnHelp());
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
                    .BindText(model => model.MoveNowText)
                    .BindColor(model => model.MoveNowColor)
                    .SetHeight(30f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
            });
            content.AddRow(row =>
            {
                row.SetHeight(MoveHintHeight);
                row.AddText()
                    .BindText(model => model.MoveHintText)
                    .SetShowBorder(false);
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.MoveThenText)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });

            AddMeter(content, model => model.DistanceText, model => model.DistancePercent, 70, 130, 230);
            AddMeter(content, model => model.TensionText, model => model.TensionPercent, 255, 140, 0);
            AddMeter(content, model => model.StaminaText, model => model.StaminaPercent, 50, 205, 50);

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Actions")
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            for (var action = 0; action < LineViewModel.ActionCount; action++)
            {
                var index = action;
                content.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(Binding<string>(LineViewModel.ActionTextProperty(index)))
                        .BindTooltip(Binding<string>(LineViewModel.ActionTooltipProperty(index)))
                        .BindIsEnabled(Binding<bool>(LineViewModel.ActionEnabledProperty(index)))
                        .BindOnClicked(model => model.OnClickAction(index))
                        .SetWidth(ActionButtonWidth)
                        .SetHeight(ActionButtonHeight);
                    row.AddLabel()
                        .BindText(Binding<string>(LineViewModel.ActionSummaryProperty(index)))
                        .BindColor(Binding<GuiColor>(LineViewModel.ActionColorProperty(index)))
                        .SetHeight(ActionButtonHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                });
            }

            content.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .BindText(model => model.CloseText)
                    .SetTooltip("Closing the window while the fish is on lets it go.")
                    .SetWidth(180f)
                    .SetHeight(34f)
                    .BindOnClicked(model => model.OnClickClose());
                row.AddSpacer();
            });
        }

        private static void AddMeter(
            GuiColumn<LineViewModel> content,
            Expression<Func<LineViewModel, string>> text,
            Expression<Func<LineViewModel, float>> percent,
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

        private static Expression<Func<LineViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(LineViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<LineViewModel, TProperty>>(property, model);
        }
    }
}
