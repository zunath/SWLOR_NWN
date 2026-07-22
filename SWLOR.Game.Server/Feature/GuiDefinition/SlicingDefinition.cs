using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SlicingDefinition : IGuiWindowDefinition
    {
        // Slicing is a fixed-size window. Its view model restores these dimensions on every open
        // so stale client geometry cannot leave the interface collapsed to a narrow title bar.
        internal const float WindowWidth = 680f;
        internal const float WindowHeight = 760f;
        private const float TileSize = 72f;
        private const float BoardHeight = TileSize * 5;
        private readonly GuiWindowBuilder<SlicingViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Slicing)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
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
                        // IMPORTANT: Keep the board as one GuiList. Multiple sibling lists in this
                        // row caused NWN NUI to collapse this fixed window to its title controls.
                        row.AddSpacer();
                        row.AddList(template =>
                        {
                            AddTileCell(template, 0);
                            AddTileCell(template, 1);
                            AddTileCell(template, 2);
                            AddTileCell(template, 3);
                            AddTileCell(template, 4);
                        })
                            .BindRowCount(model => model.TileColumn0)
                            .SetWidth(TileSize * 5)
                            .SetHeight(BoardHeight)
                            .SetRowHeight(TileSize)
                            .SetShowBorders(false)
                            .SetScrollbars(NuiScrollbars.None);
                        row.AddSpacer();
                        row.SetHeight(BoardHeight);
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
            GuiListTemplate<SlicingViewModel> template,
            int column)
        {
            template.AddCell(cell =>
            {
                cell.SetWidth(TileSize);
                cell.SetIsVariable(false);

                cell.AddGroup(group =>
                {
                    group.SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetWidth(TileSize)
                        .SetHeight(TileSize);

                    var button = group.AddButtonImage()
                        .SetHeight(TileSize)
                        .SetWidth(TileSize)
                        .SetMargin(0f);

                    switch (column)
                    {
                        case 0:
                            button.BindImageResref(model => model.TileColumn0)
                                .BindTooltip(model => model.TooltipColumn0)
                                .BindIsEnabled(model => model.EnabledColumn0)
                                .BindIsVisible(model => model.VisibleColumn0)
                                .BindOnClicked(model => model.OnTile0());
                            break;
                        case 1:
                            button.BindImageResref(model => model.TileColumn1)
                                .BindTooltip(model => model.TooltipColumn1)
                                .BindIsEnabled(model => model.EnabledColumn1)
                                .BindIsVisible(model => model.VisibleColumn1)
                                .BindOnClicked(model => model.OnTile1());
                            break;
                        case 2:
                            button.BindImageResref(model => model.TileColumn2)
                                .BindTooltip(model => model.TooltipColumn2)
                                .BindIsEnabled(model => model.EnabledColumn2)
                                .BindIsVisible(model => model.VisibleColumn2)
                                .BindOnClicked(model => model.OnTile2());
                            break;
                        case 3:
                            button.BindImageResref(model => model.TileColumn3)
                                .BindTooltip(model => model.TooltipColumn3)
                                .BindIsEnabled(model => model.EnabledColumn3)
                                .BindIsVisible(model => model.VisibleColumn3)
                                .BindOnClicked(model => model.OnTile3());
                            break;
                        case 4:
                            button.BindImageResref(model => model.TileColumn4)
                                .BindTooltip(model => model.TooltipColumn4)
                                .BindIsEnabled(model => model.EnabledColumn4)
                                .BindIsVisible(model => model.VisibleColumn4)
                                .BindOnClicked(model => model.OnTile4());
                            break;
                    }
                });
            });
        }
    }
}
