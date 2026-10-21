using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Shows, sizes, and moves the DM battle grid of the DM's current area. Opened from DM Placeable Tools.
    /// </summary>
    public class DMBattleGridDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMBattleGridViewModel> _builder = new();

        private const float ContentPanelWidth = 320f;
        private const float LabelHeight = 20f;
        private const float ActionButtonWidth = 140f;
        private const float StepButtonSize = 32f;
        private const float ValueLabelWidth = 200f;
        private const float ArrowSize = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMBattleGrid)
                .SetInitialGeometry(0f, 0f, 360f, 620f)
                .SetTitle("Battle Grid")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(DMBattleGridViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMBattleGridViewModel.MainContentElement);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMBattleGridViewModel> host)
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
                            content.AddRow(r => r.AddLabel()
                                .BindText(model => model.StatusText)
                                .SetHeight(LabelHeight)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));

                            content.AddRow(r =>
                            {
                                r.AddButton()
                                    .SetText("Place at Target")
                                    .SetTooltip("Click a spot to center the grid there. Players in the area see it.")
                                    .SetWidth(ActionButtonWidth)
                                    .BindOnClicked(model => model.OnPlaceAtTarget());
                                r.AddButton()
                                    .SetText("Place on Me")
                                    .SetTooltip("Center the grid where you stand.")
                                    .SetWidth(ActionButtonWidth)
                                    .BindOnClicked(model => model.OnPlaceHere());
                                r.AddSpacer();
                            });

                            content.AddRow(r =>
                            {
                                r.AddButton()
                                    .SetText("Remove Grid")
                                    .SetTooltip("Remove this area's grid. Its size settings are kept.")
                                    .SetWidth(ActionButtonWidth)
                                    .BindOnClicked(model => model.OnRemove())
                                    .BindIsEnabled(model => model.IsPlaced);
                                r.AddSpacer();
                            });

                            AddSectionLabel(content, "Square Size");
                            AddStepper(content, "Smaller squares", "Larger squares",
                                model => model.SquareSizeText,
                                model => model.OnSquareSizeDown(),
                                model => model.OnSquareSizeUp());

                            AddSectionLabel(content, "Width (East-West)");
                            AddStepper(content, "Narrower by 20 squares", "Wider by 20 squares",
                                model => model.WidthText,
                                model => model.OnWidthDown(),
                                model => model.OnWidthUp());

                            AddSectionLabel(content, "Length (North-South)");
                            AddStepper(content, "Shorter by 20 squares", "Longer by 20 squares",
                                model => model.LengthText,
                                model => model.OnLengthDown(),
                                model => model.OnLengthUp());

                            AddSectionLabel(content, "Height Above the Placed Spot");
                            AddStepper(content, "Lower by 0.1 m", "Raise by 0.1 m (raise it if the floor hides the grid)",
                                model => model.HeightText,
                                model => model.OnHeightDown(),
                                model => model.OnHeightUp());

                            AddSectionLabel(content, "Move One Square");
                            content.AddRow(r =>
                            {
                                AddArrow(r, "arrow_up", "North", model => model.OnMoveNorth());
                                AddArrow(r, "arrow_down", "South", model => model.OnMoveSouth());
                                AddArrow(r, "arrow_right", "East", model => model.OnMoveEast());
                                AddArrow(r, "arrow_left", "West", model => model.OnMoveWest());
                                r.AddSpacer();
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddSectionLabel(GuiColumn<DMBattleGridViewModel> content, string text)
        {
            content.AddRow(r => r.AddLabel()
                .SetText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddStepper(
            GuiColumn<DMBattleGridViewModel> content,
            string downTooltip,
            string upTooltip,
            Expression<Func<DMBattleGridViewModel, string>> valueText,
            Expression<Func<DMBattleGridViewModel, Action>> onDown,
            Expression<Func<DMBattleGridViewModel, Action>> onUp)
        {
            content.AddRow(r =>
            {
                r.AddButton()
                    .SetText("-")
                    .SetTooltip(downTooltip)
                    .SetWidth(StepButtonSize)
                    .SetHeight(StepButtonSize)
                    .BindOnClicked(onDown);
                r.AddLabel()
                    .BindText(valueText)
                    .SetWidth(ValueLabelWidth)
                    .SetHeight(StepButtonSize)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                r.AddButton()
                    .SetText("+")
                    .SetTooltip(upTooltip)
                    .SetWidth(StepButtonSize)
                    .SetHeight(StepButtonSize)
                    .BindOnClicked(onUp);
                r.AddSpacer();
            });
        }

        private static void AddArrow(
            GuiRow<DMBattleGridViewModel> row,
            string image,
            string tooltip,
            Expression<Func<DMBattleGridViewModel, Action>> onClick)
        {
            row.AddButtonImage()
                .SetImageResref(image)
                .SetTooltip(tooltip)
                .SetWidth(ArrowSize)
                .SetHeight(ArrowSize)
                .BindOnClicked(onClick)
                .BindIsEnabled(model => model.IsPlaced);
        }
    }
}
