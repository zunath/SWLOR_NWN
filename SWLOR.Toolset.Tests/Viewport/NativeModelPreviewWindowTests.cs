using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SharedModelPreviewControl = Nwn.Toolset.Avalonia.Viewport.ModelPreviewControl;
using SWLOR.Toolset.Tests.Support;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests.Viewport;

[TestFixture]
public sealed class NativeModelPreviewWindowTests
{
    [AvaloniaTest]
    public async Task LoadModelAsyncReadsInputOnUiThreadAndPublishesPreparedScene()
    {
        using var fixture = new NativePreviewWorkspaceFixture();
        var window = new NativeModelPreviewWindow(new NativeModelPreviewAdapter(fixture.Resources));
        var input = (TextBox)typeof(NativeModelPreviewWindow)
            .GetField("_resRefBox", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        input.Text = "preview_fixture";

        var operation = (Task)typeof(NativeModelPreviewWindow)
            .GetMethod("LoadModelAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null)!;
        await operation.WaitAsync(TimeSpan.FromSeconds(5));

        var viewport = (SharedModelPreviewControl)typeof(NativeModelPreviewWindow)
            .GetField("_viewport", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
        Assert.That(viewport.Scene?.ModelName, Is.EqualTo("preview_fixture"));
        Assert.That(viewport.Scene!.Nodes.Any(node => node.Mesh is { Faces.Count: > 0 }), Is.True);
        Assert.That(viewport.Textures.ContainsKey("fixture_map"), Is.True);
    }
}