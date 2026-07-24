using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SlicingDefinition : IGuiWindowDefinition
    {
        private const string HelpButtonId = "slc_help";
        private const string PreviousToolButtonId = "slc_tool_previous";
        private const string NextToolButtonId = "slc_tool_next";
        private const string ActivateToolButtonId = "slc_tool_activate";
        private const string AbortButtonId = "slc_abort";
        internal const float WindowWidth = 560f;
        internal const float WindowHeight = 650f;
        internal const float MinimumWindowWidth = 360f;
        internal const float MinimumWindowHeight = 240f;
        private const float TileSize = 56f;
        private const string HelpText =
            "OBJECTIVE\n" +
            "Create one continuous powered circuit from the amber START / Entry tile to the magenta GOAL / Core tile. " +
            "Connected tiles glow yellow. You win as soon as power reaches the Core; decoy tiles do not need to be connected.\n\n" +
            "CONTROLS\n" +
            "- Click any unselected tile to select it. Selection is free and is shown by a bright diamond outline.\n" +
            "- Click the selected tile again to rotate it clockwise. This costs 1 Trace.\n" +
            "- Click a tile directly above, below, left, or right of the selected tile to swap them. This costs 2 Trace. " +
            "START and GOAL are fixed sockets; they cannot be rotated or swapped.\n" +
            "- Click any other non-adjacent tile to move the selection for free.\n" +
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
                row.SetHeight(78f);
                row.AddImage()
                    .BindResref(model => model.ThemeBackground)
                    .SetAspect(NuiAspect.Fit)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetHeight(78f);
            });

            column.AddRow(row =>
            {
                row.SetHeight(30f);
                row.AddLabel().BindText(model => model.TraceText).SetWidth(100f).SetHeight(28f);
                row.AddLabel().BindText(model => model.IntegrityText).SetWidth(120f).SetHeight(28f);
                row.AddSpacer();
                AddHelpButton(row);
            });
            column.AddRow(row =>
            {
                row.SetHeight(30f);
                row.AddText()
                    .BindText(model => model.FailureText)
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None)
                    .SetHeight(28f);
            });

            column.AddRow(row =>
            {
                row.SetHeight(40f);
                row.AddText()
                    .SetText("GOAL: Connect the amber START tile to the magenta GOAL tile.")
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None);
            });
            column.AddRow(row =>
            {
                row.SetHeight(80f);
                row.AddText()
                    .SetText("Select any tile for free. Click it again to rotate (1 Trace), or click an adjacent tile to swap (2 Trace).")
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
                row.SetHeight(42f);
                row.AddButton()
                    .SetId(PreviousToolButtonId)
                    .SetText("<")
                    .SetWidth(36f)
                    .SetHeight(34f)
                    .BindIsEnabled(model => model.IsToolSelectionEnabled)
                    .BindOnClicked(model => model.OnPreviousTool());
                row.AddText()
                    .BindText(model => model.ToolName)
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None)
                    .SetHeight(40f);
                row.AddButton()
                    .SetId(NextToolButtonId)
                    .SetText(">")
                    .SetWidth(36f)
                    .SetHeight(34f)
                    .BindIsEnabled(model => model.IsToolSelectionEnabled)
                    .BindOnClicked(model => model.OnNextTool());
            });

            column.AddRow(row =>
            {
                row.SetHeight(42f);
                row.AddSpacer();
                row.AddButton()
                    .SetId(ActivateToolButtonId)
                    .SetText("Activate Tool")
                    .SetWidth(120f)
                    .SetHeight(34f)
                    .BindIsEnabled(model => model.IsToolActivationEnabled)
                    .BindOnClicked(model => model.OnActivateTool());
                row.AddSpacer();
            });

            column.AddRow(row =>
            {
                row.SetHeight(56f);
                row.AddText()
                    .BindText(model => model.StatusText)
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None);
            });

            column.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetId(AbortButtonId)
                    .SetText("Abort")
                    .SetWidth(140f)
                    .SetHeight(38f)
                    .BindOnClicked(model => model.OnAbort());
                row.AddSpacer();
            });
        }

        private static void AddHelpButton(GuiRow<SlicingViewModel> row)
        {
            row.AddButton()
                .SetId(HelpButtonId)
                .SetText("?")
                .SetTooltip("How slicing works")
                .SetWidth(34f)
                .SetHeight(28f)
                .BindOnClicked(model => model.OnHelp());
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
                    .SetId($"slc_tile_{slot}")
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
