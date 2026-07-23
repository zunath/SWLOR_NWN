using System.Linq.Expressions;
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
                        // IMPORTANT: Keep this as a static row/column board. GuiList-based versions of
                        // this particular window have repeatedly collapsed to only their title controls
                        // in the live NWN client. Do not reintroduce list templates or vector visibility
                        // binds here; this window intentionally uses scalar-bound tiles and columns.
                        row.AddSpacer();
                        for (var tileColumn = 0; tileColumn < 5; tileColumn++)
                            AddTileColumn(row, tileColumn);
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

        private static void AddTileColumn(GuiRow<SlicingViewModel> row, int column)
        {
            row.AddColumn(tileColumn =>
            {
                for (var tileRow = 0; tileRow < 5; tileRow++)
                {
                    var slot = tileRow * 5 + column;
                    tileColumn.AddRow(tileRowDefinition =>
                    {
                        tileRowDefinition.AddButtonImage()
                            .BindImageResref(Binding<string>($"TileImage{slot}"))
                            .BindTooltip(Binding<string>($"TileTooltip{slot}"))
                            .BindOnClicked(model => model.OnTile(tileRow, column))
                            .SetHeight(TileSize)
                            .SetWidth(TileSize)
                            .SetMargin(0f);
                        tileRowDefinition.SetHeight(TileSize);
                    });
                }
            })
                .BindIsVisible(Binding<bool>($"IsColumn{column}Visible"))
                .SetWidth(TileSize)
                .SetHeight(BoardHeight);
        }

        private static Expression<Func<SlicingViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(SlicingViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<SlicingViewModel, TProperty>>(property, model);
        }
    }
}
