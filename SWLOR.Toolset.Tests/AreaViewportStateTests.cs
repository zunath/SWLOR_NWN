using System.Numerics;
using System.Reflection;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests
{
    public sealed class AreaViewportStateTests
    {
        [Test]
        public void CreatureModelsDrawTwoSidedWithoutDisablingPropCulling()
        {
            var policy = typeof(GlAreaControl).GetMethod(
                "CullInstanceModelFaces",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            policy.Invoke(null, [InstanceMarkerKind.Creature]).Should().Be(false,
                "segmented creature equipment has mixed winding and otherwise disappears in the area view");
            policy.Invoke(null, [InstanceMarkerKind.Placeable]).Should().Be(true,
                "dense areas still need ordinary prop face culling");
            policy.Invoke(null, [InstanceMarkerKind.Door]).Should().Be(true);
        }

        [Test]
        public void StoreMarkersUseAurorasWaypointYellow()
        {
            var markerColor = typeof(GlAreaControl).GetMethod(
                "MarkerColor",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var store = (Vector3)markerColor.Invoke(null, [InstanceMarkerKind.Store])!;
            var waypoint = (Vector3)markerColor.Invoke(null, [InstanceMarkerKind.Waypoint])!;

            store.Should().Be(new Vector3(0.98f, 0.80f, 0.10f));
            store.Should().Be(waypoint, "Aurora draws merchants as yellow waypoint markers");
        }

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
            replacementControl.Scene = new AreaScene
            {
                Tileset = "tcn01",
                Width = 8,
                Height = 8,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics()
            };

            replacementControl.CaptureViewportState().Should().Be(expected,
                "the first scene must establish a rebuild baseline without replacing restored camera state");
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

            var focused = control.CaptureViewportState()!.Value;
            focused.Target.Should().Be(
                expectedTarget,
                "the first scene's default framing must not overwrite a Go To request made while it loaded");
            focused.Distance.Should().BeLessThanOrEqualTo(15f,
                "Go To must zoom from the full-area framing to an object-scale view");
        }

        [AvaloniaTest]
        public void DeferredGoToFocusAppliedAfterViewportRestore_Wins()
        {
            var expectedTarget = new Vector3(17f, 29f, 3f);
            var control = new GlAreaControl
            {
                Scene = new AreaScene
                {
                    Tileset = "tcn01",
                    Width = 8,
                    Height = 8,
                    Tiles = Array.Empty<TilePlacement>(),
                    Instances = Array.Empty<InstanceMarker>(),
                    Diagnostics = new AreaSceneDiagnostics()
                }
            };

            control.RestoreViewportState(new AreaViewportState(
                new Vector3(-40f, -50f, 1f),
                Distance: 22f,
                InitialDistance: 33f,
                Azimuth: 0.5f,
                Elevation: 0.7f));
            control.FocusOn(expectedTarget);

            control.CaptureViewportState()!.Value.Target.Should().Be(expectedTarget,
                "the retained viewport must be restored before a queued Source-tab Go To is applied");
        }
    }
}
