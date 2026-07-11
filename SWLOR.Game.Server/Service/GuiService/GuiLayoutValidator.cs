using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Reports layout shapes confirmed to fail in-game via the DebugNuiGallery hazard harness
    /// (/nuigallery); suspected-but-unconfirmed shapes are deliberately NOT flagged, because zero
    /// `[NUI layout warning]` lines is a hard authoring gate—every warning must be a real defect.
    /// The bar for adding a new rule: a confirmed-failing hazard partial in DebugNuiGallery first.
    /// See Readmes/NuiLayoutRules.md.
    /// </summary>
    public static class GuiLayoutValidator
    {
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
                Walk(
                    partial,
                    $"{windowId} > partial '{partialName}'",
                    findings);
            }

            return findings;
        }

        private static void Walk(
            IGuiWidget widget,
            string path,
            List<string> findings)
        {
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

            for (var index = 0; index < widget.Elements.Count; index++)
            {
                var child = widget.Elements[index];

                Walk(
                    child,
                    $"{path} > {DescribeWidget(child)}[{index}]",
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
