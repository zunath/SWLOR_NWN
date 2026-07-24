using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SlicingDefinition : IGuiWindowDefinition
    {
        internal const float WindowWidth = 560f;
        internal const float WindowHeight = 650f;
        internal const float MinimumWindowWidth = 320f;
        internal const float MinimumWindowHeight = 240f;
        private const float TileSize = 56f;
        private const string HelpText =
            "OBJECTIVE\n" +
            "Create one continuous powered circuit from the amber START / Entry tile to the magenta GOAL / Core tile. " +
            "Connected tiles glow yellow. You win as soon as power reaches the Core; decoy tiles do not need to be connected.\n\n" +
            "CONTROLS\n" +
            "- Click any different tile to select it, including an adjacent tile. Selection is free and is shown by a bright diamond outline.\n" +
            "- Click the selected tile again to rotate it clockwise. This costs 1 Trace.\n" +
            "- To swap, select a movable tile, click Swap Tile, then click a tile directly above, below, left, or right. This costs 2 Trace. " +
            "Click Cancel Swap if you change your mind. " +
            "START and GOAL are fixed sockets; they cannot be rotated or swapped.\n" +
            "There is no double-click action.\n\n" +
            "TRACE AND FAILURE\n" +
            "Trace is your action budget. Reach the Core before it runs out. Slicing rank, Lockpicking, and positive Perception can grant extra Trace. " +
            "A rotation or swap commits the attempt. An immediate tool effect also commits it; a primed tool commits when its effect is consumed. " +
            "Before commitment, closing or aborting is safe. After commitment, running out of Trace, closing, or aborting counts as a failure and raises " +
            "the risk that the target is destroyed.\n\n" +
            "TOOLS\n" +
            "You may use one compatible slicing tool per attempt. Tools can reduce action costs, restore Trace, reveal route " +
            "information, correct a tile, or rewind actions. Some tools require a selected tile.";
        private readonly GuiWindowBuilder<SlicingViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Slicing)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Slicing Interface")
                .SetIsResizable(true)
                .SetIsCollapsible(false)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(SlicingViewModel.HelpPartial, AddHelp)
                .AddRow(wrapperRow =>
                {
                    // This full static body must be present in the initial window JSON. Replacing
                    // it with an empty partial and populating that partial at runtime causes the
                    // live client to collapse the entire window to its title bar.
                    wrapperRow.AddGroup(wrapper =>
                    {
                        wrapper.SetShowBorder(false)
                            .SetScrollbars(NuiScrollbars.Auto);
                        wrapper.AddColumn(AddMainContent);
                    });
                });

            return _builder.Build();
        }

        private static void AddMainContent(GuiColumn<SlicingViewModel> column)
        {
            column.AddRow(row =>
            {
                row.AddImage()
                    .BindResref(model => model.ThemeBackground)
                    .SetHeight(78f)
                    // This is the proven width anchor for the entire static content canvas.
                    // Removing it collapses the live NUI layout to the top-left corner.
                    .SetWidth(520f);
            });

            column.AddRow(row =>
            {
                row.AddLabel().BindText(model => model.TraceText).SetWidth(100f).SetHeight(28f);
                row.AddLabel().BindText(model => model.IntegrityText).SetWidth(110f).SetHeight(28f);
                row.AddLabel().BindText(model => model.FailureText).SetWidth(230f).SetHeight(28f);
                row.AddButton()
                    .SetText("?")
                    .SetTooltip("How slicing works")
                    .SetWidth(34f)
                    .SetHeight(28f)
                    .BindOnClicked(model => model.OnHelp());
            });

            column.AddRow(row =>
            {
                row.SetHeight(64f);
                row.AddText()
                    .SetText("GOAL: Connect amber START to magenta GOAL.\nClick selected again to rotate (1 Trace). To swap (2 Trace), click Swap Tile, then an adjacent tile.")
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None);
            });

            // Keep the board as five ordinary rows of five buttons. Previous list-based
            // layouts and the transposed row-of-columns layout collapse in the live NUI
            // client before the view model can populate their binds.
            for (var tileRow = 0; tileRow < 5; tileRow++)
            {
                var rowIndex = tileRow;
                column.AddRow(row => AddTileRow(row, rowIndex));
            }

            column.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .BindText(model => model.SwapButtonText)
                    .SetTooltip("Arm a deliberate swap, then choose a directly adjacent tile")
                    .SetWidth(160f)
                    .SetHeight(34f)
                    .BindIsEnabled(model => model.IsSwapEnabled)
                    .BindOnClicked(model => model.OnSwap());
                row.AddSpacer();
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
                row.AddButton().SetText("Activate Tool").SetWidth(120f).SetHeight(34f)
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
        }

        private static void AddHelp(GuiGroup<SlicingViewModel> group)
        {
            group.SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.Auto);
            group.AddColumn(column =>
            {
                column.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("How Slicing Works")
                        .SetHeight(32f);
                });
                column.AddRow(row =>
                {
                    row.SetHeight(68f);
                    row.AddText()
                        .BindText(model => model.BoardText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None);
                });
                column.AddRow(row =>
                {
                    row.AddText()
                        .SetText(HelpText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto);
                });
                column.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Close")
                        .SetWidth(120f)
                        .SetHeight(36f)
                        .BindOnClicked(model => model.OnCloseHelp());
                    row.AddSpacer();
                });
            });
        }

        private static void AddTileRow(GuiRow<SlicingViewModel> row, int tileRow)
        {
            row.AddSpacer();
            for (var tileColumn = 0; tileColumn < 5; tileColumn++)
            {
                var columnIndex = tileColumn;
                var slot = tileRow * 5 + columnIndex;
                row.AddButtonImage()
                    .BindImageResref(Binding<string>($"TileImage{slot}"))
                    .BindTooltip(Binding<string>($"TileTooltip{slot}"))
                    .BindOnClicked(model => model.OnTile(tileRow, columnIndex))
                    .SetHeight(TileSize)
                    .SetWidth(TileSize)
                    .SetMargin(0f);
            }
            row.AddSpacer();
            row.SetHeight(TileSize);
        }

        private static Expression<Func<SlicingViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(SlicingViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<SlicingViewModel, TProperty>>(property, model);
        }
    }
}
