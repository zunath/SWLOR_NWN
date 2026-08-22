using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Whether a remembered window position is still worth restoring.
    /// </summary>
    /// <remarks>
    /// A saved position outlives the monitor it was saved on. Undock a laptop, restart, and the
    /// coordinates point at a display that is no longer there - restoring them puts the window where the
    /// builder can neither see it nor drag it back.
    /// </remarks>
    [TestFixture]
    public class WindowPlacementScreenTests
    {
        private static readonly ScreenBounds Primary = new(0, 0, 1920, 1080);

        /// <summary>A second monitor to the left, which is where negative coordinates come from.</summary>
        private static readonly ScreenBounds LeftHand = new(-1920, 0, 1920, 1080);

        private static WindowPlacement At(double left, double top) =>
            new(1600, 900, left, top, false);

        [Test]
        public void APositionOnAConnectedScreenIsRestored()
        {
            At(240, 120).IsOnAnyScreen(new[] { Primary }).Should().BeTrue();
        }

        [Test]
        public void APositionOnASecondMonitorIsRestored()
        {
            At(-1500, 200).IsOnAnyScreen(new[] { Primary, LeftHand }).Should().BeTrue();
        }

        [Test]
        public void APositionOnAMonitorThatIsGoneIsNotRestored()
        {
            At(-1500, 200).IsOnAnyScreen(new[] { Primary }).Should().BeFalse();
        }

        [Test]
        public void AWindowParkedPastTheRightEdgeIsNotRestored()
        {
            // Its title bar would be off the screen, so there would be nothing left to drag it back by.
            At(1900, 500).IsOnAnyScreen(new[] { Primary }).Should().BeFalse();
        }

        [Test]
        public void ASmallOverhangPastTheTopLeftIsStillRestored()
        {
            At(-6, -4).IsOnAnyScreen(new[] { Primary }).Should().BeTrue("a window pushed against the edge is deliberate");
        }

        [Test]
        public void AParkedMinimizedPositionIsNotRestored()
        {
            // Windows reports a minimised window at (-32000, -32000).
            At(-32000, -32000).IsOnAnyScreen(new[] { Primary }).Should().BeFalse();
        }

        [Test]
        public void APlacementWithNoPositionIsNotRestored()
        {
            WindowPlacement.Unset.IsOnAnyScreen(new[] { Primary }).Should().BeFalse();
        }

        [Test]
        public void WhenTheScreensCannotBeReadTheSavedPositionIsTrusted()
        {
            At(240, 120).IsOnAnyScreen(Array.Empty<ScreenBounds>()).Should()
                .BeTrue("failing to enumerate screens is not evidence the position is bad");
        }
    }
}
