using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A garden plot's window: the crop, its stage and time left, a picker for the fertilizer and one for the seeds the player
    /// carries, and the four actions (Plant, Tend, Harvest and Clear). Anyone can open it to look; each action is enabled only when the player may
    /// do it, with the reason as its tooltip.
    /// </summary>
    public class GardenPlotDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<GardenPlotViewModel> _builder = new();

        private const float ContentPanelWidth = 420f;
        private const float LabelHeight = 20f;
        private const float MessageHeight = 56f;
        private const float SeedRowHeight = 32f;
        private const float FertilizerLabelWidth = 90f;
        private const float FertilizerComboWidth = 320f;
        private const float FertilizerComboHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.GardenPlot)
                .SetInitialGeometry(0, 0, 480f, 500f)
                .SetTitle("Garden Plot")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(GardenPlotViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(GardenPlotViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<GardenPlotViewModel> host)
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
                            AddStatusLine(content, model => model.CropText, GuiColor.Cyan);
                            AddStatusLine(content, model => model.PlanterText);
                            AddStatusLine(content, model => model.StageText);
                            AddStatusLine(content, model => model.TimeLeftText);

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.MessageText)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(MessageHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Plant")
                                    .BindIsEnabled(model => model.CanPlant)
                                    .BindDisabledTooltip(model => model.PlantTooltip)
                                    .BindOnClicked(model => model.OnClickPlant());
                                line.AddButton()
                                    .SetText("Tend")
                                    .BindIsEnabled(model => model.CanTend)
                                    .BindDisabledTooltip(model => model.TendTooltip)
                                    .BindOnClicked(model => model.OnClickTend());
                                line.AddButton()
                                    .SetText("Harvest")
                                    .BindIsEnabled(model => model.CanHarvest)
                                    .BindDisabledTooltip(model => model.HarvestTooltip)
                                    .BindOnClicked(model => model.OnClickHarvest());
                                line.AddButton()
                                    .SetText("Clear")
                                    .BindIsEnabled(model => model.CanClear)
                                    .BindDisabledTooltip(model => model.ClearTooltip)
                                    .BindOnClicked(model => model.OnClickClear());
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .SetText("Fertilizer:")
                                    .SetWidth(FertilizerLabelWidth)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                line.AddComboBox()
                                    .BindOptions(model => model.FertilizerOptions)
                                    .BindSelectedIndex(model => model.SelectedFertilizerIndex)
                                    .BindIsEnabled(model => model.CanPickFertilizer)
                                    .SetTooltip("Fertilizer of the crop's tier or higher adds 1 item to the harvest. It is used up when you plant.")
                                    .SetWidth(FertilizerComboWidth)
                                    .SetHeight(FertilizerComboHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.SeedHeaderText)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetColor(GuiColor.Cyan);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.SeedNames)
                                            .BindIsToggled(model => model.SeedToggles)
                                            .BindOnClicked(model => model.OnClickSeed());
                                    });
                                })
                                    .BindRowCount(model => model.SeedNames)
                                    .SetRowHeight(SeedRowHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddStatusLine(
            GuiColumn<GardenPlotViewModel> content,
            System.Linq.Expressions.Expression<System.Func<GardenPlotViewModel, string>> text,
            GuiColor color = null)
        {
            content.AddRow(line =>
            {
                var label = line.AddLabel()
                    .BindText(text)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (color != null)
                    label.SetColor(color);
            });
        }
    }
}
