using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SlicingDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SlicingViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Slicing)
                .SetInitialGeometry(0, 0, 680f, 760f)
                .SetTitle("Slicing Interface")
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .BindOnClosed(model => model.OnWindowClosed())
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddImage()
                            .BindResref(model => model.ThemeBackground)
                            .SetHeight(96f)
                            .SetWidth(640f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel().BindText(model => model.TraceText).SetHeight(24f);
                        row.AddLabel().BindText(model => model.IntegrityText).SetHeight(24f);
                        row.AddLabel().BindText(model => model.FailureText).SetHeight(24f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Click a tile to select it. Click it again to rotate clockwise (1 trace), or click an adjacent tile to swap (2 trace).")
                            .SetHeight(38f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            AddTileCell(template, 0);
                            AddTileCell(template, 1);
                            AddTileCell(template, 2);
                            AddTileCell(template, 3);
                            AddTileCell(template, 4);
                        })
                            .BindRowCount(model => model.TileColumn0)
                            .SetHeight(390f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddButton().SetText("<").SetWidth(36f).SetHeight(34f)
                            .BindIsEnabled(model => model.IsToolSelectionEnabled)
                            .BindOnClicked(model => model.OnPreviousTool());
                        row.AddLabel().BindText(model => model.ToolName).SetHeight(34f);
                        row.AddButton().SetText(">").SetWidth(36f).SetHeight(34f)
                            .BindIsEnabled(model => model.IsToolSelectionEnabled)
                            .BindOnClicked(model => model.OnNextTool());
                        row.AddButton().SetText("Activate Tool").SetWidth(130f).SetHeight(34f)
                            .BindIsEnabled(model => model.IsToolActivationEnabled)
                            .BindOnClicked(model => model.OnActivateTool());
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel().BindText(model => model.StatusText).SetHeight(36f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton().SetText("Abort").SetWidth(140f).SetHeight(38f)
                            .BindOnClicked(model => model.OnAbort());
                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }

        private static void AddTileCell(
            Service.GuiService.Component.GuiListTemplate<SlicingViewModel> template,
            int column)
        {
            template.AddCell(cell =>
            {
                var button = cell.AddButtonImage()
                    .SetHeight(72f)
                    .SetWidth(72f);

                switch (column)
                {
                    case 0:
                        button.BindImageResref(model => model.TileColumn0)
                            .BindTooltip(model => model.TooltipColumn0)
                            .BindIsEnabled(model => model.EnabledColumn0)
                            .BindOnClicked(model => model.OnTile0());
                        break;
                    case 1:
                        button.BindImageResref(model => model.TileColumn1)
                            .BindTooltip(model => model.TooltipColumn1)
                            .BindIsEnabled(model => model.EnabledColumn1)
                            .BindOnClicked(model => model.OnTile1());
                        break;
                    case 2:
                        button.BindImageResref(model => model.TileColumn2)
                            .BindTooltip(model => model.TooltipColumn2)
                            .BindIsEnabled(model => model.EnabledColumn2)
                            .BindOnClicked(model => model.OnTile2());
                        break;
                    case 3:
                        button.BindImageResref(model => model.TileColumn3)
                            .BindTooltip(model => model.TooltipColumn3)
                            .BindIsEnabled(model => model.EnabledColumn3)
                            .BindOnClicked(model => model.OnTile3());
                        break;
                    case 4:
                        button.BindImageResref(model => model.TileColumn4)
                            .BindTooltip(model => model.TooltipColumn4)
                            .BindIsEnabled(model => model.EnabledColumn4)
                            .BindOnClicked(model => model.OnTile4());
                        break;
                }
            });
        }
    }
}
