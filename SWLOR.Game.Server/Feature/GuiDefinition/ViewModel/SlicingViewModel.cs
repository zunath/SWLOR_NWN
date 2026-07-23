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
        private readonly List<SlicingSession.EligibleSlicingTool> _tools = new();
        private int _toolIndex;
        private bool _suppressCloseFailure;

        public string ThemeBackground { get => Get<string>(); set => Set(value); }
        public string TraceText { get => Get<string>(); set => Set(value); }
        public string IntegrityText { get => Get<string>(); set => Set(value); }
        public string FailureText { get => Get<string>(); set => Set(value); }
        public string ToolName { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public bool IsToolSelectionEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsToolActivationEnabled { get => Get<bool>(); set => Set(value); }

        public bool IsColumn0Visible => Get<bool>();
        public bool IsColumn1Visible => Get<bool>();
        public bool IsColumn2Visible => Get<bool>();
        public bool IsColumn3Visible => Get<bool>();
        public bool IsColumn4Visible => Get<bool>();

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
            RestoreFixedWindowGeometry();
            // The base view swap schedules its own zero-delay geometry redraw, so repair once more after it settles.
            DelayCommand(0.0f, RestoreFixedWindowGeometry);
            _suppressCloseFailure = false;
            _toolIndex = 0;
            StatusText = string.Empty;
            Refresh();
        }

        public Action OnTile(int row, int column) => () => ClickTile(row, column);

        public Action OnPreviousTool() => () => ChangeTool(-1);
        public Action OnNextTool() => () => ChangeTool(1);

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

            string message;
            if (session.SelectedIndex == index)
            {
                SlicingSession.RotateSelected(Player, out message);
            }
            else if (session.SelectedIndex >= 0 && Slicing.AreAdjacent(session.Board, session.SelectedIndex, index))
            {
                SlicingSession.SwapSelectedWith(Player, index, out message);
            }
            else
            {
                SlicingSession.SelectTile(Player, index, out message);
                if (string.IsNullOrWhiteSpace(message))
                    message = "Tile selected.";
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

        private void RestoreFixedWindowGeometry()
        {
            // Keep this recovery local to Slicing; do not generalize it into Gui.
            // A forced close can leave this reused fixed window with narrow client geometry.
            var currentGeometry = Geometry;
            Geometry = new GuiRectangle(
                currentGeometry?.X ?? 0f,
                currentGeometry?.Y ?? 0f,
                SlicingDefinition.WindowWidth,
                SlicingDefinition.WindowHeight);
        }

        private void Refresh()
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
                return;

            ThemeBackground = session.Source == SlicingSourceType.Lockbox ? "slc_bg_l" : "slc_bg_t";
            TraceText = $"Trace: {session.TraceRemaining}";
            var integrity = SlicingSession.GetIntegrity(session.Target);
            IntegrityText = $"Integrity: {integrity}%";
            var failureNumber = SlicingSession.GetFailures(session.Target) + 1;
            FailureText = $"Failure {failureNumber}: {Slicing.GetDestructionChance(failureNumber)}% break risk";

            var powered = Slicing.GetPoweredIndices(session.Board);
            for (var column = 0; column < 5; column++)
                Set(column < session.Board.Width, $"IsColumn{column}Visible");

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
            var state = integrity <= 50
                ? 'd'
                : session.SelectedIndex == index
                    ? 's'
                    : powered
                        ? 'p'
                        : 'u';
            return $"slc{theme}{type}{tile.Orientation}{state}";
        }

        private static string GetTileTooltip(SlicingSession.ActiveSlicingSession session, int index)
        {
            var parts = new List<string> { $"Tile {index + 1}" };
            var tile = session.Board.Tiles[index];
            if (tile.IsRouteRevealed)
                parts.Add("Verified route tile");
            if (tile.IsOrientationRevealed)
            {
                var clockwise = (tile.SolutionOrientation - tile.Orientation + 4) % 4;
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
    }
}
