using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Walks constructed window widget trees before serialization and reports layout
    /// shapes known or suspected to make NUI's constraint solver fail on the client
    /// ("NuiSetLayout failed: The constraint can not be satisfied"). The client error
    /// carries no element context, so surfacing findings at window-build time (server
    /// boot) with a widget path is the only way to make these diagnosable.
    ///
    /// Findings are advisory: they are returned to the caller (and logged at boot)
    /// rather than thrown, because the exact failure rules of the engine's Cassowary
    /// solver are not fully documented. Rules here are derived from the engine layout
    /// source and from auditing every shipping window. See Readmes/NuiLayoutRules.md.
    /// </summary>
    public static class GuiLayoutValidator
    {
        private const string WindowMainPartial = "%%WINDOW_MAIN%%";
        private const string WindowModalPartial = "%%WINDOW_MODAL%%";
        private const string WindowInputModalPartial = "%%WINDOW_INPUT_MODAL%%";

        /// <summary>
        /// Findings at these exact widget paths are confirmed false positives: the windows
        /// that produce them ship and render correctly in-game today. See
        /// Readmes/NuiLayoutRules.md R2 "Known counterexamples that ship and work". Paths
        /// must match the "windowId > partial '...' > Widget[index] > ..." format built by
        /// <see cref="Walk"/> exactly, up to (and including) the flagged widget.
        ///
        /// Do not add an entry here without verifying the specific window in-game first —
        /// this list exists to keep boot output trustworthy for real defects, not to silence
        /// warnings that are merely inconvenient.
        /// </summary>
        private static readonly string[] AcknowledgedFindingPaths =
        {
            // Unbounded non-terminal list; AppearanceEditor's part list. Confirmed working in-game.
            "GUI_WINDOW_AppearanceEditor > partial 'APPEARANCE_EDITOR_MAIN_PARTIAL' > GuiColumn[0] > GuiRow[1] > GuiColumn[0] > GuiRow[0] > GuiList[0]",

            // Unbounded non-terminal list; AppearanceEditor's color list. Confirmed working in-game.
            "GUI_WINDOW_AppearanceEditor > partial 'APPEARANCE_EDITOR_MAIN_PARTIAL' > GuiColumn[0] > GuiRow[1] > GuiColumn[1] > GuiRow[1] > GuiList[0]",

            // Unbounded non-terminal list; DMPlayerExamine's Notes view. Confirmed working in-game.
            "GUI_WINDOW_DMPlayerExamine > partial 'NOTES_VIEW' > GuiColumn[0] > GuiRow[0] > GuiColumn[0] > GuiRow[0] > GuiList[0]",

            // Unbounded non-terminal list; Settings' Chat view. Confirmed working in-game.
            "GUI_WINDOW_Settings > partial 'CHAT_VIEW' > GuiColumn[0] > GuiRow[0] > GuiList[0]",
        };

        /// <summary>
        /// Validates every partial view of a constructed window.
        /// </summary>
        /// <param name="windowId">The window id, used to prefix finding paths.</param>
        /// <param name="partialViews">All partial views defined on the window, including the main/modal wrappers.</param>
        /// <returns>Human-readable findings; empty when nothing suspicious was found.</returns>
        public static List<string> Validate(string windowId, IReadOnlyDictionary<string, IGuiWidget> partialViews)
        {
            var findings = new List<string>();

            foreach (var (partialName, partial) in partialViews)
            {
                // The main and modal wrappers are swapped into the special "_window_" root
                // group, which is hard-bounded by the window's Geometry rect. Named partials
                // land in ordinary unsized groups, where unbounded content is riskier.
                var isNamedPartial = partialName != WindowMainPartial &&
                                     partialName != WindowModalPartial &&
                                     partialName != WindowInputModalPartial;

                Walk(
                    partial,
                    $"{windowId} > partial '{partialName}'",
                    isNamedPartial,
                    isHeightBounded: false,
                    isTerminal: true,
                    findings);
            }

            // Drop findings that are confirmed false positives for specific, already-verified
            // windows so the warning channel stays trustworthy for new/real defects. A finding
            // is always "{path}: message", so matching "{acknowledgedPath}:" only matches the
            // acknowledged widget itself, not deeper descendants that happen to share a prefix.
            findings.RemoveAll(finding =>
                AcknowledgedFindingPaths.Any(acknowledgedPath => finding.StartsWith(acknowledgedPath + ":")));

            return findings;
        }

        private static void Walk(
            IGuiWidget widget,
            string path,
            bool inNamedPartial,
            bool isHeightBounded,
            bool isTerminal,
            List<string> findings)
        {
            if (widget.DeclaredHeight > 0f)
                isHeightBounded = true;

            // A scrollable container decouples its content from parent constraints:
            // overflow scrolls instead of failing the solve, so anything inside is
            // effectively bounded by the container's viewport.
            if (widget is IGuiScrollable scrollable && scrollable.Scrollbars != NuiScrollbars.None)
                isHeightBounded = true;

            // A list template cell that is fixed-width but has no width gives the solver
            // a cell it can neither size nor grow. But when the cell's own inner element
            // declares a positive width (e.g. a 32f-wide button), the solver sizes the cell
            // from that element instead, and the layout is solvable. This shape ships in
            // CharacterFullRebuildDefinition's skill-point rows (four cells wrapping
            // +1/+10/-1/-10 buttons with SetWidth(32f) but no width on the cell itself) and
            // renders correctly, so only flag cells where nothing declares a usable width.
            if (widget is IGuiTemplateCell cell &&
                !cell.IsVariable &&
                widget.DeclaredWidth <= 0f &&
                !(widget.Elements.Count > 0 && widget.Elements[0].DeclaredWidth > 0f))
            {
                findings.Add(
                    $"{path}: template cell is fixed (isVariable: false) but declares no width, and its inner " +
                    "element has no declared width either. Give the cell (or its inner element) a positive " +
                    "width, or mark the cell variable.");
            }

            // A list advertises no size to its parent. Every working nested partial in the
            // codebase either bounds its lists (explicit height on the list or an ancestor)
            // or places them terminally so nothing depends on space below them. A list that
            // is neither is the shape under investigation for client-side constraint errors.
            if (inNamedPartial &&
                IsWidgetOfType(widget, typeof(GuiList<>)) &&
                !isHeightBounded &&
                widget.DeclaredHeight <= 0f &&
                !isTerminal)
            {
                findings.Add(
                    $"{path}: list has no explicit height and rows follow it inside a nested partial. " +
                    "Bound it (SetHeight on the list, its row, or a wrapping group) or move it to be the last row of its column.");
            }

            // A row with an explicit height must leave room for its children's default
            // margins: the engine's REQUIRED constraint is row_height >= child_height
            // + margins, and buttons carry a nonzero default margin. A fixed row whose
            // buttons are the same height (or taller) is therefore unsolvable. This is
            // the confirmed root cause of the HoloCom window's layout failures; no
            // shipping window uses this shape. (The tabbar widget has no margin, which
            // is why CharacterSheet's equal-height toggle rows are fine.)
            if (IsWidgetOfType(widget, typeof(GuiRow<>)) && widget.DeclaredHeight > 0f)
            {
                foreach (var child in widget.Elements)
                {
                    var isButtonFamily =
                        IsWidgetOfType(child, typeof(GuiButton<>)) ||
                        IsWidgetOfType(child, typeof(GuiButtonImage<>)) ||
                        IsWidgetOfType(child, typeof(GuiToggleButton<>));

                    if (isButtonFamily && child.DeclaredHeight >= widget.DeclaredHeight)
                    {
                        findings.Add(
                            $"{path}: row has explicit height {widget.DeclaredHeight} but contains a " +
                            $"{DescribeWidget(child)} of height {child.DeclaredHeight}, leaving no room for the " +
                            "widget's default margins (row_height >= child_height + margins is required). " +
                            "Remove the row's SetHeight and let it derive from its children.");
                    }
                }
            }

            var isColumn = IsWidgetOfType(widget, typeof(GuiColumn<>));

            for (var index = 0; index < widget.Elements.Count; index++)
            {
                var child = widget.Elements[index];

                // Only descending into a non-last row of a column makes content non-terminal:
                // vertical space below it is claimed by later rows. Horizontal position within
                // a row does not affect height solvability.
                var childIsTerminal = isTerminal && (!isColumn || index == widget.Elements.Count - 1);

                Walk(
                    child,
                    $"{path} > {DescribeWidget(child)}[{index}]",
                    inNamedPartial,
                    isHeightBounded,
                    childIsTerminal,
                    findings);
            }
        }

        private static bool IsWidgetOfType(IGuiWidget widget, Type openGenericType)
        {
            var type = widget.GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
        }

        private static string DescribeWidget(IGuiWidget widget)
        {
            var name = widget.GetType().Name;
            var backtickIndex = name.IndexOf('`');
            return backtickIndex >= 0 ? name[..backtickIndex] : name;
        }
    }
}
