using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PropertyDiagnosticsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PropertyDiagnosticsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PropertyDiagnostics)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 720f, 420f)
                .SetTitle("Property Diagnostics")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHeight(24f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.PropertyRows)
                                    .BindIsToggled(model => model.PropertySelections)
                                    .BindTooltip(model => model.PropertyTooltips)
                                    .BindOnClicked(model => model.OnSelectProperty());
                            });
                        })
                            .SetScrollbars(NuiScrollbars.Auto)
                            .BindRowCount(model => model.PropertyRows);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Refresh")
                            .BindOnClicked(model => model.OnRefresh());

                        row.AddButton()
                            .SetText("Retry Load")
                            .BindIsEnabled(model => model.IsPropertySelected)
                            .BindOnClicked(model => model.OnRetryLoad());

                        row.AddButton()
                            .SetText("Abort Queue")
                            .BindIsEnabled(model => model.IsPropertySelected)
                            .BindOnClicked(model => model.OnAbortQueue());

                        row.AddButton()
                            .SetText("Notify")
                            .BindIsEnabled(model => model.IsPropertySelected)
                            .BindOnClicked(model => model.OnNotifyWaiters());
                    });
                });

            return _builder.Build();
        }
    }
}
