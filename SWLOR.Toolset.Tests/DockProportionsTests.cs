using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Shell;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Carrying the shell's divider positions between sessions.
    /// </summary>
    /// <remarks>
    /// The layout is built in code every startup, so without this the panel widths a builder set were
    /// rebuilt from the designed defaults every time - the dividers looked like they simply did not save.
    /// </remarks>
    [TestFixture]
    public class DockProportionsTests
    {
        /// <summary>The shell's shape: explorer / documents / palette across, output underneath.</summary>
        private static ProportionalDock BuildLayout()
        {
            var middle = new ProportionalDock
            {
                Id = "MiddleLayout",
                Proportion = 0.72,
                VisibleDockables = new List<IDockable>
                {
                    new ToolDock { Id = "ExplorerDock", Proportion = 0.28 },
                    new ProportionalDockSplitter(),
                    new DocumentDock { Id = "Documents", Proportion = 0.47 },
                    new ProportionalDockSplitter(),
                    new ToolDock { Id = "PaletteDock", Proportion = 0.27 }
                }
            };

            return new ProportionalDock
            {
                Id = "MainLayout",
                VisibleDockables = new List<IDockable>
                {
                    middle,
                    new ProportionalDockSplitter(),
                    new ToolDock { Id = "OutputDock", Proportion = 0.20 }
                }
            };
        }

        private static IDockable Find(IDockable root, string id) =>
            DockProportions.Walk(root).Single(dockable => dockable.Id == id);

        [Test]
        public void CaptureRecordsEveryNestedDock()
        {
            var captured = DockProportions.Capture(BuildLayout());

            captured.Should().ContainKeys("MiddleLayout", "ExplorerDock", "Documents", "PaletteDock", "OutputDock");
            captured["ExplorerDock"].Should().BeApproximately(0.28, 0.0001);
            captured["OutputDock"].Should().BeApproximately(0.20, 0.0001);
        }

        [Test]
        public void CaptureLeavesOutDocksThatWereNeverSized()
        {
            var layout = BuildLayout();

            // Dock's own splitters carry no Id, and the root was never given a proportion - both would
            // otherwise be written to the settings file as an empty key or a NaN.
            var captured = DockProportions.Capture(layout);

            captured.Should().NotContainKey("");
            captured.Should().NotContainKey("MainLayout");
            captured.Values.Should().OnlyContain(value => double.IsFinite(value));
        }

        [Test]
        public void ApplyPutsTheDividersBackWhereTheyWereLeft()
        {
            var layout = BuildLayout();

            DockProportions.Apply(layout, new Dictionary<string, double>
            {
                ["ExplorerDock"] = 0.4,
                ["OutputDock"] = 0.35
            });

            Find(layout, "ExplorerDock").Proportion.Should().BeApproximately(0.4, 0.0001);
            Find(layout, "OutputDock").Proportion.Should().BeApproximately(0.35, 0.0001);
        }

        [Test]
        public void ADockWithNothingSavedKeepsItsDesignedSize()
        {
            var layout = BuildLayout();

            DockProportions.Apply(layout, new Dictionary<string, double> { ["ExplorerDock"] = 0.4 });

            Find(layout, "PaletteDock").Proportion.Should().BeApproximately(0.27, 0.0001,
                "a panel added since the last session has no saved entry and must keep its design");
        }

        [Test]
        public void ASavedEntryForADockThatIsGoneIsIgnored()
        {
            var layout = BuildLayout();

            var act = () => DockProportions.Apply(layout, new Dictionary<string, double>
            {
                ["PropertiesDock"] = 0.3,
                ["ExplorerDock"] = 0.4
            });

            act.Should().NotThrow("the layout is free to change without the settings file holding it back");
            Find(layout, "ExplorerDock").Proportion.Should().BeApproximately(0.4, 0.0001);
        }

        [Test]
        public void AnUnusableSavedProportionIsIgnoredRatherThanCollapsingAPanel()
        {
            var layout = BuildLayout();

            DockProportions.Apply(layout, new Dictionary<string, double>
            {
                ["ExplorerDock"] = 0,
                ["PaletteDock"] = 1.5,
                ["OutputDock"] = double.NaN
            });

            Find(layout, "ExplorerDock").Proportion.Should().BeApproximately(0.28, 0.0001);
            Find(layout, "PaletteDock").Proportion.Should().BeApproximately(0.27, 0.0001);
            Find(layout, "OutputDock").Proportion.Should().BeApproximately(0.20, 0.0001);
        }

        [Test]
        public void ApplyThenCaptureRoundTrips()
        {
            var layout = BuildLayout();
            var saved = new Dictionary<string, double>
            {
                ["MiddleLayout"] = 0.8,
                ["ExplorerDock"] = 0.19,
                ["Documents"] = 0.55,
                ["PaletteDock"] = 0.24,
                ["OutputDock"] = 0.12
            };

            DockProportions.Apply(layout, saved);

            DockProportions.Capture(layout).Should().Contain(saved);
        }

        [Test]
        public void MovingADividerReportsTheChange()
        {
            var layout = BuildLayout();
            var changes = 0;

            DockProportions.Watch(layout, () => changes++);
            // What Dock does to the model when a splitter is dragged.
            Find(layout, "ExplorerDock").Proportion = 0.4;

            changes.Should().Be(1);
        }
    }
}
