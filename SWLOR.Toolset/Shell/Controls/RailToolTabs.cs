using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Core;
using AvaloniaDock = Avalonia.Controls.Dock;

namespace SWLOR.Toolset.Shell.Controls
{
    /// <summary>
    /// Makes the left rail's tool tabs the panel's own title bar: moved above the panel, and marked
    /// so the theme can dress them as titles rather than as Dock's default chrome.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dock's <c>ToolControl</c> template is a DockPanel with the tab strip docked Bottom, and the
    /// strip is its first child, so moving the tabs is a matter of re-docking that one element - the
    /// content border is the fill child either way.
    /// </para>
    /// <para>
    /// Scoped by dock Id rather than styled across the theme on purpose. Module Contents and Area
    /// Contents are two views of the same question and get switched between constantly, so their
    /// tabs belong where the eye already is. Palette/Script Reference and the Output group are
    /// consulted rather than lived in, and their tabs are better off out of the way at the bottom
    /// where Dock puts them. A style on <c>ToolControl</c> would have moved all three.
    /// </para>
    /// <para>
    /// A class handler rather than a walk of the shell's visual tree, because a ToolControl is built
    /// whenever its dock is templated - which happens again every time a panel is floated, docked
    /// somewhere else, or pinned - and each new one has to be caught.
    /// </para>
    /// </remarks>
    public static class RailToolTabs
    {
        /// <summary>The style class the theme hangs the rail tab appearance off.</summary>
        public const string StripClass = "rail";

        /// <summary>The docks whose tabs stand in for their panel's title.</summary>
        private static readonly HashSet<string> RailDockIds =
            new(StringComparer.Ordinal) { "ExplorerDock" };

        private static bool _registered;

        /// <summary>Starts watching for tool docks. Safe to call more than once.</summary>
        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            TemplatedControl.TemplateAppliedEvent.AddClassHandler<ToolControl>(OnTemplateApplied);
        }

        private static void OnTemplateApplied(ToolControl control, TemplateAppliedEventArgs e)
        {
            Apply(control);

            // The dock a ToolControl shows can change without the template being rebuilt, and which
            // dock it is decides how the strip is drawn. Unsubscribed first so a re-templated control
            // does not end up with the handler twice.
            control.DataContextChanged -= OnDataContextChanged;
            control.DataContextChanged += OnDataContextChanged;
        }

        private static void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (sender is ToolControl control)
                Apply(control);
        }

        private static void Apply(ToolControl control)
        {
            if (control.GetVisualDescendants().OfType<ToolTabStrip>().FirstOrDefault() is not { } strip)
                return;

            var isRail = IsRail(control);

            // Both directions are set explicitly. Bottom is Dock's own default, and a panel dragged
            // out of the left rail and dropped elsewhere would otherwise keep the title-bar tabs it
            // had there.
            DockPanel.SetDock(strip, isRail ? AvaloniaDock.Top : AvaloniaDock.Bottom);

            if (isRail)
                strip.Classes.Add(StripClass);
            else
                strip.Classes.Remove(StripClass);
        }

        private static bool IsRail(ToolControl control) =>
            control.DataContext is IDock { Id: { } id } && RailDockIds.Contains(id);
    }
}
