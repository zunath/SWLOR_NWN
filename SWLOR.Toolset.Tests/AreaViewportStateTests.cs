using System.Numerics;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests
{
    public sealed class AreaViewportStateTests
    {
        [AvaloniaTest]
        public void ViewportState_RoundTripsAcrossControlRecreation()
        {
            var expected = new AreaViewportState(
                new Vector3(12.5f, -4f, 8f),
                Distance: 30f,
                InitialDistance: 50f,
                Azimuth: 1.25f,
                Elevation: 0.45f);
            var replacementControl = new GlAreaControl();

            replacementControl.RestoreViewportState(expected);

            replacementControl.CaptureViewportState().Should().Be(expected);
        }
    }
}
