using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SlicingViewModel : GuiViewModelBase<SlicingViewModel, SlicingPayload>
    {
        public const string HelpPartial = "SLICING_HELP";

        private readonly List<SlicingSession.EligibleSlicingTool> _tools = new();
        private int _toolIndex;
        private bool _isSwapMode;
        private bool _suppressCloseFailure;

        public string ThemeBackground { get => Get<string>(); set => Set(value); }
        public string TraceText { get => Get<string>(); set => Set(value); }
        public string IntegrityText { get => Get<string>(); set => Set(value); }
        public string FailureText { get => Get<string>(); set => Set(value); }
        public string BoardText { get => Get<string>(); set => Set(value); }
        public string SwapButtonText { get => Get<string>(); set => Set(value); }
        public string ToolName { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public bool IsSwapEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsToolSelectionEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsToolActivationEnabled { get => Get<bool>(); set => Set(value); }

        public string TileImage0 => Get<string>();
        public string TileImage1 => Get<string>();
        public string TileImage2 => Get<string>();
        public string TileImage3 => Get<string>();
        public string TileImage4 => Get<string>();
        public string TileImage5 => Get<string>();
        public string TileImage6 => Get<string>();
        public string TileImage7 => Get<string>();
        public string TileImage8 => Get<string>();
        public string TileImage9 => Get<string>();
        public string TileImage10 => Get<string>();
        public string TileImage11 => Get<string>();
        public string TileImage12 => Get<string>();
        public string TileImage13 => Get<string>();
        public string TileImage14 => Get<string>();
        public string TileImage15 => Get<string>();
        public string TileImage16 => Get<string>();
        public string TileImage17 => Get<string>();
        public string TileImage18 => Get<string>();
        public string TileImage19 => Get<string>();
        public string TileImage20 => Get<string>();
        public string TileImage21 => Get<string>();
        public string TileImage22 => Get<string>();
        public string TileImage23 => Get<string>();
        public string TileImage24 => Get<string>();

        public string TileTooltip0 => Get<string>();
        public string TileTooltip1 => Get<string>();
        public string TileTooltip2 => Get<string>();
        public string TileTooltip3 => Get<string>();
        public string TileTooltip4 => Get<string>();
        public string TileTooltip5 => Get<string>();
        public string TileTooltip6 => Get<string>();
        public string TileTooltip7 => Get<string>();
        public string TileTooltip8 => Get<string>();
        public string TileTooltip9 => Get<string>();
        public string TileTooltip10 => Get<string>();
        public string TileTooltip11 => Get<string>();
        public string TileTooltip12 => Get<string>();
        public string TileTooltip13 => Get<string>();
        public string TileTooltip14 => Get<string>();
        public string TileTooltip15 => Get<string>();
        public string TileTooltip16 => Get<string>();
        public string TileTooltip17 => Get<string>();
        public string TileTooltip18 => Get<string>();
        public string TileTooltip19 => Get<string>();
        public string TileTooltip20 => Get<string>();
        public string TileTooltip21 => Get<string>();
        public string TileTooltip22 => Get<string>();
        public string TileTooltip23 => Get<string>();
        public string TileTooltip24 => Get<string>();

        protected override void Initialize(SlicingPayload initialPayload)
        {
            EnsureUsableWindowGeometry();
            // The base partial-view swap schedules its own zero-delay geometry redraw. Reapply the
            // minimum afterward so a legacy title-bar-sized geometry cannot lose a pixel and persist.
            DelayCommand(0.0f, EnsureUsableWindowGeometry);
            _suppressCloseFailure = false;
            _toolIndex = 0;
            _isSwapMode = false;
            StatusText = string.Empty;
            Refresh();
        }

        public Action OnTile(int row, int column) => () => ClickTile(row, column);
        public Action OnHelp() => () => ChangeView(HelpPartial);
        public Action OnCloseHelp() => () =>
        {
            ChangeView("%%WINDOW_MAIN%%");
            Refresh();
        };

        public Action OnPreviousTool() => () => ChangeTool(-1);
        public Action OnNextTool() => () => ChangeTool(1);

        public Action OnSwap() => () =>
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
            {
                CloseWindow();
                return;
            }

            if (_isSwapMode)
            {
                _isSwapMode = false;
                StatusText = "Swap cancelled.";
                Refresh();
                return;
            }

            if (!CanBeginSwap(session))
            {
                StatusText = session.SelectedIndex < 0
                    ? "Select a movable tile first."
                    : "START and GOAL sockets are fixed and cannot be swapped.";
                Refresh();
                return;
            }

            _isSwapMode = true;
            StatusText = $"Swap armed for Tile {session.SelectedIndex + 1}. Choose a directly adjacent tile.";
            Refresh();
        };

        public Action OnActivateTool() => () =>
        {
            if (_tools.Count == 0 || _toolIndex < 0 || _toolIndex >= _tools.Count)
                return;

            SlicingSession.ActivateTool(Player, _tools[_toolIndex].Item, out var message);
            StatusText = message;
            Refresh();
            CloseIfEnded();
        };

        public Action OnAbort() => () =>
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
            {
                CloseWindow();
                return;
            }

            if (!session.HasCommitted)
            {
                SlicingSession.Abort(Player);
                CloseWindow();
                return;
            }

            var failureNumber = SlicingSession.GetFailures(session.Target) + 1;
            var destructionChance = Slicing.GetDestructionChance(failureNumber);
            UpdatePropertyFromClient(nameof(Geometry));
            ShowModal(
                $"Abort this committed attempt? It will count as failure {failureNumber} and has a {destructionChance}% chance to destroy the target.",
                () =>
                {
                    SlicingSession.Abort(Player);
                    CloseWindow();
                },
                confirmText: "Abort",
                cancelText: "Continue");
        };

        public override Action OnWindowClosed() => () =>
        {
            if (!_suppressCloseFailure)
                SlicingSession.Abort(Player);
        };

        private void ClickTile(int row, int column)
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
            {
                CloseWindow();
                return;
            }

            var index = row * session.Board.Width + column;
            if (row >= session.Board.Height || column >= session.Board.Width || index >= session.Board.Tiles.Count)
                return;

            var action = DetermineTileClickAction(
                _isSwapMode,
                session.SelectedIndex,
                index,
                session.SelectedIndex >= 0 && Slicing.AreAdjacent(session.Board, session.SelectedIndex, index));

            string message;
            switch (action)
            {
                case TileClickAction.Rotate:
                    SlicingSession.RotateSelected(Player, out message);
                    break;
                case TileClickAction.Swap:
                    if (SlicingSession.SwapSelectedWith(Player, index, out message))
                        _isSwapMode = false;
                    break;
                case TileClickAction.InvalidSwap:
                    message = index == session.SelectedIndex
                        ? "Choose a different tile directly above, below, left, or right, or click Cancel Swap."
                        : "That tile is not directly adjacent. Choose another tile or click Cancel Swap.";
                    break;
                case TileClickAction.Select:
                    SlicingSession.SelectTile(Player, index, out message);
                    if (string.IsNullOrWhiteSpace(message))
                        message = $"Tile {index + 1} selected.";
                    break;
                default:
                    message = "That tile action is invalid.";
                    break;
            }

            StatusText = message;
            Refresh();
            CloseIfEnded();
        }

        private void ChangeTool(int direction)
        {
            if (_tools.Count == 0)
                return;

            _toolIndex = (_toolIndex + direction + _tools.Count) % _tools.Count;
            RefreshToolDisplay();
        }

        private void ChangeView(string partialName)
        {
            // Capture a resize before the partial-view redraw workaround mutates the geometry bind.
            UpdatePropertyFromClient(nameof(Geometry));
            ChangePartialView("_window_", partialName);
        }

        private void EnsureUsableWindowGeometry()
        {
            var current = Geometry;
            if (current == null ||
                current.Width >= SlicingDefinition.MinimumWindowWidth &&
                current.Height >= SlicingDefinition.MinimumWindowHeight)
                return;

            Geometry = new GuiRectangle(
                current.X,
                current.Y,
                Math.Max(current.Width, SlicingDefinition.MinimumWindowWidth),
                Math.Max(current.Height, SlicingDefinition.MinimumWindowHeight));
        }

        private void Refresh()
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
                return;

            ThemeBackground = session.Source == SlicingSourceType.Lockbox ? "slc_goal_l" : "slc_goal_t";
            TraceText = $"Trace: {session.TraceRemaining}";
            var integrity = SlicingSession.GetIntegrity(session.Target);
            IntegrityText = $"Integrity: {integrity}%";
            var failureNumber = SlicingSession.GetFailures(session.Target) + 1;
            FailureText = $"Failure {failureNumber}: {Slicing.GetDestructionChance(failureNumber)}% break risk";
            BoardText = $"BOARD ID: {session.Board.BoardId} - include this ID when reporting unexpected board behavior.";
            SwapButtonText = _isSwapMode ? "Cancel Swap" : "Swap Tile (2 Trace)";
            IsSwapEnabled = _isSwapMode || CanBeginSwap(session);

            var powered = Slicing.GetPoweredIndices(session.Board);
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 5; column++)
                {
                    var slot = row * 5 + column;
                    if (row >= session.Board.Height || column >= session.Board.Width)
                    {
                        SetTileBinding(slot, "Blank", string.Empty);
                        continue;
                    }

                    var index = row * session.Board.Width + column;
                    SetTileBinding(
                        slot,
                        GetTileImage(session, index, powered.Contains(index), integrity),
                        GetTileTooltip(session, index));
                }
            }

            _tools.Clear();
            _tools.AddRange(SlicingSession.GetEligibleTools(Player));
            if (_toolIndex >= _tools.Count)
                _toolIndex = 0;
            RefreshToolDisplay();
        }

        private static bool CanBeginSwap(SlicingSession.ActiveSlicingSession session)
        {
            return session.SelectedIndex >= 0 &&
                   session.SelectedIndex < session.Board.Tiles.Count &&
                   session.Board.Tiles[session.SelectedIndex].Type is not
                       (SlicingTileType.Entry or SlicingTileType.Core);
        }

        private static TileClickAction DetermineTileClickAction(
            bool isSwapMode,
            int selectedIndex,
            int clickedIndex,
            bool isAdjacent)
        {
            if (isSwapMode)
                return selectedIndex >= 0 && clickedIndex != selectedIndex && isAdjacent
                    ? TileClickAction.Swap
                    : TileClickAction.InvalidSwap;

            return selectedIndex == clickedIndex
                ? TileClickAction.Rotate
                : TileClickAction.Select;
        }

        private void RefreshToolDisplay()
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
                return;

            if (session.HasUsedTool)
            {
                ToolName = session.PrimedTool == SlicingToolType.Invalid
                    ? "Slicing tool already used"
                    : $"Used/primed: {FormatToolName(session.PrimedTool)}";
                IsToolSelectionEnabled = false;
                IsToolActivationEnabled = false;
                return;
            }

            if (_tools.Count == 0)
            {
                ToolName = "No compatible tool in inventory";
                IsToolSelectionEnabled = false;
                IsToolActivationEnabled = false;
                return;
            }

            ToolName = $"{_tools[_toolIndex].Name} ({_toolIndex + 1}/{_tools.Count})";
            IsToolSelectionEnabled = _tools.Count > 1;
            IsToolActivationEnabled = true;
        }

        private static string GetTileImage(
            SlicingSession.ActiveSlicingSession session,
            int index,
            bool powered,
            int integrity)
        {
            var tile = session.Board.Tiles[index];
            var theme = session.Source == SlicingSourceType.Lockbox ? 'l' : 't';
            var type = tile.Type switch
            {
                SlicingTileType.Straight => 's',
                SlicingTileType.Corner => 'c',
                SlicingTileType.Junction => 'j',
                SlicingTileType.Cross => 'x',
                SlicingTileType.Entry => 'e',
                SlicingTileType.Core => 'o',
                SlicingTileType.Blocker => 'b',
                _ => 'q'
            };
            var state = session.SelectedIndex == index
                ? 's'
                : integrity <= 50
                    ? 'd'
                    : powered
                        ? 'p'
                        : 'u';
            var family = tile.Type is SlicingTileType.Entry or SlicingTileType.Core
                ? "slcg"
                : "slc";
            return $"{family}{theme}{type}{tile.Orientation}{state}";
        }

        private static string GetTileTooltip(SlicingSession.ActiveSlicingSession session, int index)
        {
            var tile = session.Board.Tiles[index];
            var identity = tile.Type switch
            {
                SlicingTileType.Entry => "START / Entry - Fixed socket",
                SlicingTileType.Core => "GOAL / Core - Fixed socket",
                _ => $"Tile {index + 1}"
            };
            var parts = new List<string> { identity };
            if (tile.IsRouteRevealed)
                parts.Add("Verified route tile");
            if (tile.IsOrientationRevealed)
            {
                var clockwise = Slicing.GetClockwiseSolutionRotationCost(tile);
                parts.Add($"Correct orientation: {clockwise} clockwise turn(s)");
            }

            return string.Join(" - ", parts);
        }

        private void SetTileBinding(int slot, string image, string tooltip)
        {
            Set(image, $"TileImage{slot}");
            Set(tooltip, $"TileTooltip{slot}");
        }

        private static string FormatToolName(SlicingToolType type)
        {
            return type.ToString()
                .Replace("TraceFuse", "Trace Fuse")
                .Replace("Bypass", " Bypass ")
                .Replace("Servo", " Servo ")
                .Replace("Shunt", " Shunt ")
                .Replace("TraceSplice", " Trace Splice")
                .Replace("Signature", " Signature ")
                .Replace("Sampler", " Sampler")
                .Replace("Spectrograph", " Spectrograph")
                .Replace("Decoder", " Decoder")
                .Replace("Prism", " Prism")
                .Replace("Oracle", " Oracle");
        }

        private void CloseIfEnded()
        {
            if (SlicingSession.Get(Player) == null)
                CloseWindow();
        }

        private void CloseWindow()
        {
            _suppressCloseFailure = true;
            Gui.TogglePlayerWindow(Player, GuiWindowType.Slicing);
        }

        private enum TileClickAction
        {
            Select,
            Rotate,
            Swap,
            InvalidSwap
        }
    }
}
