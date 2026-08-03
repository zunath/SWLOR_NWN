using System.Numerics;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using SWLOR.Toolset.Domain.Render;
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

        [AvaloniaTest]
        public void FocusRequestedBeforeInitialScene_IsAppliedAfterSceneFraming()
        {
            var expectedTarget = new Vector3(73f, 41f, 2.5f);
            var control = new GlAreaControl();

            control.FocusOn(expectedTarget);
            control.Scene = new AreaScene
            {
                Tileset = "tcn01",
                Width = 8,
                Height = 8,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics()
            };

            control.CaptureViewportState()!.Value.Target.Should().Be(
                expectedTarget,
                "the first scene's default framing must not overwrite a Go To request made while it loaded");
        }
    }
}
