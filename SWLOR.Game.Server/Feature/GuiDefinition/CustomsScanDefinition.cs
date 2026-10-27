using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A customs patrol's hail (plan section 13): what the scan found, the fine, and the pilot's answer. Slicing the
    /// scan comes with space slicing (W12) and is shown disabled.
    /// </summary>
    public class CustomsScanDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CustomsScanViewModel> _builder = new();

        private const float ContentPanelWidth = 420f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 35f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CustomsScan)
                .SetInitialGeometry(0, 0, 470f, 300f)
                .SetTitle("Customs Scan")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(CustomsScanViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(CustomsScanViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<CustomsScanViewModel> host)
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
                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.PatrolText)
                                    .SetColor(GuiColor.Cyan)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.FoundText)
                                    .SetHeight(70f);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.FineText)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.DeadlineText)
                                    .SetColor(GuiColor.Red)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Pay Fine")
                                    .SetTooltip("Pay the fine and surrender the goods the patrol bans. Lawful standing drops a little.")
                                    .BindIsEnabled(model => model.CanPay)
                                    .BindOnClicked(model => model.OnClickPay())
                                    .SetHeight(ButtonHeight);
                                line.AddButton()
                                    .SetText("Run")
                                    .SetTooltip("Keep your cargo and run. The patrol turns hostile and lawful standing drops more.")
                                    .BindIsEnabled(model => model.IsPending)
                                    .BindOnClicked(model => model.OnClickRun())
                                    .SetHeight(ButtonHeight);
                                line.AddButton()
                                    .SetText("Slice the Scan")
                                    .SetTooltip("Comes with space slicing")
                                    .BindIsEnabled(model => model.CanSlice)
                                    .SetHeight(ButtonHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }
    }
}
