using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Mvvm.Controls;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Shell.Panels;
using AvaloniaDock = Avalonia.Controls.Dock;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Where each tool dock draws its tab strip.
    /// </summary>
    /// <remarks>
    /// The left rail's tabs are moved to the top by re-docking one element of Dock's own
    /// ToolControl template. That is exactly the kind of change that stops working silently when the
    /// Dock package moves its parts around, and the kind that quietly spreads to every other dock if
    /// someone reaches for a theme-wide style later - so both halves are asserted here.
    /// </remarks>
    [NonParallelizable]
    public class RailToolTabsTests
    {
        [AvaloniaTest]
        public void TheLeftRailDrawsItsTabsAboveThePanel()
        {
            StripDockOf("ExplorerDock").Should().Be(
                AvaloniaDock.Top,
                "Module Contents and Area Contents are switched between constantly, so their tabs " +
                "belong next to the panel title rather than at the far end of the rail");
        }

        [AvaloniaTest]
        public void EveryOtherDockKeepsItsTabsWhereDockPutsThem()
        {
            StripDockOf("PaletteDock").Should().Be(AvaloniaDock.Bottom);
            StripDockOf("OutputDock").Should().Be(AvaloniaDock.Bottom);
        }

        /// <summary>
        /// Builds one tool dock the way the shell does, renders it, and reports which edge its tab
        /// strip ended up on.
        /// </summary>
        private static AvaloniaDock StripDockOf(string dockId)
        {
            var dock = new ToolDock
            {
                Id = dockId,
                VisibleDockables = new List<Dock.Model.Core.IDockable>
                {
                    new AreaContentsViewModel(),
                    new AreaContentsViewModel { Id = "Second", Title = "Second" }
                }
            };
            dock.ActiveDockable = dock.VisibleDockables[0];

            var control = new ToolControl { DataContext = dock };
            var window = new Window { Width = 400, Height = 600, Content = control };

            window.Show();
            Dispatcher.UIThread.RunJobs();

            var strip = control.GetVisualDescendants().OfType<ToolTabStrip>().FirstOrDefault();
            strip.Should().NotBeNull(
                "the placement fix has nothing to move if Dock stopped building a ToolTabStrip here");

            var placement = DockPanel.GetDock(strip!);

            window.Close();
            Dispatcher.UIThread.RunJobs();

            return placement;
        }
    }
}
