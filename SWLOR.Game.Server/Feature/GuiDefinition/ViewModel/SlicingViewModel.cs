using System.Collections.Generic;
using System.Linq;
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

        public GuiBindingList<string> TileColumn0 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TileColumn1 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TileColumn2 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TileColumn3 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TileColumn4 { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public GuiBindingList<string> TooltipColumn0 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TooltipColumn1 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TooltipColumn2 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TooltipColumn3 { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TooltipColumn4 { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public GuiBindingList<bool> EnabledColumn0 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> EnabledColumn1 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> EnabledColumn2 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> EnabledColumn3 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> EnabledColumn4 { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public GuiBindingList<bool> VisibleColumn0 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> VisibleColumn1 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> VisibleColumn2 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> VisibleColumn3 { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> VisibleColumn4 { get => Get<GuiBindingList<bool>>(); set => Set(value); }

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

        public Action OnTile0() => () => ClickTile(0);
        public Action OnTile1() => () => ClickTile(1);
        public Action OnTile2() => () => ClickTile(2);
        public Action OnTile3() => () => ClickTile(3);
        public Action OnTile4() => () => ClickTile(4);

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

        private void ClickTile(int column)
        {
            var session = SlicingSession.Get(Player);
            if (session == null)
            {
                CloseWindow();
                return;
            }

            var row = NuiGetEventArrayIndex();
            var index = row * session.Board.Width + column;
            if (column >= session.Board.Width || index >= session.Board.Tiles.Count)
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
            var images = CreateStringColumns();
            var tooltips = CreateStringColumns();
            var enabled = CreateBoolColumns();
            var visible = CreateBoolColumns();

            for (var row = 0; row < session.Board.Height; row++)
            {
                for (var column = 0; column < 5; column++)
                {
                    var isVisible = column < session.Board.Width;
                    visible[column].Add(isVisible);
                    if (!isVisible)
                    {
                        images[column].Add("Blank");
                        tooltips[column].Add(string.Empty);
                        enabled[column].Add(false);
                        continue;
                    }

                    var index = row * session.Board.Width + column;
                    images[column].Add(GetTileImage(session, index, powered.Contains(index), integrity));
                    tooltips[column].Add(GetTileTooltip(session, index));
                    enabled[column].Add(true);
                }
            }

            TileColumn0 = images[0]; TileColumn1 = images[1]; TileColumn2 = images[2]; TileColumn3 = images[3]; TileColumn4 = images[4];
            TooltipColumn0 = tooltips[0]; TooltipColumn1 = tooltips[1]; TooltipColumn2 = tooltips[2]; TooltipColumn3 = tooltips[3]; TooltipColumn4 = tooltips[4];
            EnabledColumn0 = enabled[0]; EnabledColumn1 = enabled[1]; EnabledColumn2 = enabled[2]; EnabledColumn3 = enabled[3]; EnabledColumn4 = enabled[4];
            VisibleColumn0 = visible[0]; VisibleColumn1 = visible[1]; VisibleColumn2 = visible[2]; VisibleColumn3 = visible[3]; VisibleColumn4 = visible[4];

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

        private static GuiBindingList<string>[] CreateStringColumns()
        {
            return Enumerable.Range(0, 5).Select(_ => new GuiBindingList<string>()).ToArray();
        }

        private static GuiBindingList<bool>[] CreateBoolColumns()
        {
            return Enumerable.Range(0, 5).Select(_ => new GuiBindingList<bool>()).ToArray();
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
