using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Numerics;
using Nwn.Preview.Areas;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.PreviewRender.Viewport;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.PreviewRender.Application;

internal sealed class NativeViewportApplication : global::Avalonia.Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("A real desktop is required.");
        var root = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT")
            ?? throw new InvalidOperationException("Select the corpus with SWLOR_HAKS_ROOT.");
        var output = Environment.GetEnvironmentVariable("SWLOR_PREVIEW_RENDER_OUTPUT")
            ?? throw new InvalidOperationException("Select the owned screenshot with SWLOR_PREVIEW_RENDER_OUTPUT.");
        var resources = new ResourceIndex(null, [new("chest", Path.Combine(root, "sw_pt_chest")),
            new("materials", Path.Combine(root, "sw_tint_mtr")), new("mask", Path.Combine(root, "sw_tint0")),
            new("palette", Path.Combine(root, "sw_item"))]);
        resources.InitializationTask.GetAwaiter().GetResult();
        var model = new TileModelCache(resources).GetOrBuild("pfa0_chest001")
            ?? throw new InvalidOperationException("The production chest model must resolve.");
        if (model.Meshes.Count == 0)
            throw new InvalidOperationException("The production chest must contain renderable meshes.");
        Console.WriteLine($"pfa0_chest001 meshes={model.Meshes.Count} faces={model.Meshes.Sum(mesh => mesh.Indices.Length / 3)}");
        var scene = new AreaScene
        {
            Tileset = string.Empty,
            Width = 10,
            Height = 10,
            Tiles = Array.Empty<TilePlacement>(),
            Instances =
            [
                new InstanceMarker
                {
                    Kind = InstanceMarkerKind.Placeable,
                    TemplateResRef = "pfa0_chest001",
                    Position = new Vector3(5f, 5f, 0f),
                    Orientation = Vector2.UnitX,
                    Model = model
                }
            ],
            Diagnostics = new AreaSceneDiagnostics()
        };
        var provider = new SwlorAreaViewportMaterialProvider(resources);
        var surface = new TintReadbackSurface(scene, provider, output);
        surface.Completed += (passed, message) => Dispatcher.UIThread.Post(() =>
        { Console.WriteLine(message); desktop.Shutdown(passed ? 0 : 1); });
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) =>
        { timer.Stop(); Console.Error.WriteLine("The native material viewport exceeded fifteen seconds."); desktop.Shutdown(2); };
        desktop.Exit += (_, _) => timer.Stop();
        timer.Start();
        desktop.MainWindow = new Window { Title = "SWLOR material viewport qualification", Width = 800, Height = 600, Content = surface };
        base.OnFrameworkInitializationCompleted();
    }
}
