using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Numerics;
using Avalonia.VisualTree;
using FluentAssertions;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Preview.Areas;
using NUnit.Framework;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests;

[NonParallelizable]
public sealed class AreaEditorSharedLayoutTests
{
    [AvaloniaTest]
    public void ExistingAreaViewUsesSharedTabsWithoutReplacingItsSceneOrProperties()
    {
        var view = new AreaEditorView();
        var window = new Window { Width = 1200, Height = 850, Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var layout = view.FindControl<AreaEditorLayout>("RootTabs")!;
            var scene = view.FindControl<AreaSceneView>("SceneView")!;
            var surface = scene.Surface;
            var properties = view.FindControl<ScrollViewer>("PropertiesScroll")!;
            var camera = scene.CameraControls;
            camera.Viewport.Should().BeSameAs(surface.Viewport);
            camera.GetVisualDescendants().OfType<Button>().Should().HaveCount(11);
            layout.Scene.Should().BeSameAs(scene);
            scene.GetVisualDescendants().Should().Contain(surface);
            surface.Bounds.Height.Should().BeGreaterThan(650);
            layout.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            layout.Properties.Should().BeSameAs(properties);
            layout.GetVisualDescendants().Should().Contain(properties);
            layout.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            surface.Bounds.Height.Should().BeGreaterThan(650);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
    [AvaloniaTest]
    public void ExistingSceneRotationButtonClickRoutesToTheViewportPreview()
    {
        var view = new AreaEditorView();
        var window = new Window { Width = 1200, Height = 850, Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var scene = view.FindControl<AreaSceneView>("SceneView")!;
            scene.Overlay = new AreaSceneOverlay { CanRotateSelection = true, HasSceneSelection = true };
            var selected = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Placeable,
                TemplateResRef = "crate",
                Tag = "fixture_crate",
                Position = new Vector3(1, 2, 0),
                Orientation = new Vector2(1, 0),
            };
            scene.Viewport.SelectedInstance = selected;

            InstanceMarker? preview = null;
            scene.Viewport.ManipulationPreviewChanged += (_, current) => preview = current;
            var clockwise = scene.GetVisualDescendants().OfType<RepeatButton>()
                .Single(button => button.Content?.ToString() == "⟳");
            clockwise.IsEnabled.Should().BeTrue();
            clockwise.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

            preview.Should().NotBeNull("the shared scene view owns the rotation pad event");
            preview!.Orientation.Y.Should().BeLessThan(0,
                "a clockwise click should update the selected instance's live orientation preview");
            preview.Position.Should().Be(selected.Position);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
    [AvaloniaTest]
    public void SharedCameraPadActionChangesTheExistingHostViewportState()
    {
        var view = new AreaEditorView();
        var window = new Window { Width = 1200, Height = 850, Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var scene = view.FindControl<AreaSceneView>("SceneView")!;
            scene.Viewport.Scene = new AreaScene
            {
                Tileset = "fixture",
                Width = 2,
                Height = 2,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics(),
            };
            var initial = scene.Viewport.CaptureViewportState()!.Value;
            var orbitLeft = scene.CameraControls.GetVisualDescendants().OfType<RepeatButton>()
                .Single(button => button.Content?.ToString() == "↺");
            orbitLeft.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            scene.Viewport.CaptureViewportState()!.Value.Azimuth.Should().BeLessThan(initial.Azimuth);
            scene.Viewport.Scene.Should().NotBeNull("camera actions must keep using the editor's current area scene");
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
