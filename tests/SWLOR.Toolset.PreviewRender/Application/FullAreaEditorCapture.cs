using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Toolset.Avalonia.Areas;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.PreviewRender.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.PreviewRender.Application;

internal static class FullAreaEditorCapture
{
    private static readonly TimeSpan CaptureDelay = TimeSpan.FromSeconds(2);

    public static void Start(
        IClassicDesktopStyleApplicationLifetime desktop,
        ResourceIndex resources,
        string sceneOutputPath)
    {
        var output = Path.GetFullPath(sceneOutputPath);
        var propertiesOutput = Path.Combine(
            Path.GetDirectoryName(output)!,
            Path.GetFileNameWithoutExtension(output) + "-properties" + Path.GetExtension(output));
        var repositoryRoot = Environment.GetEnvironmentVariable("SWLOR_TEST_REPOSITORY_ROOT")
            ?? throw new InvalidOperationException("Select the real SWLOR module with SWLOR_TEST_REPOSITORY_ROOT.");
        var moduleRoot = Path.Combine(Path.GetFullPath(repositoryRoot), "Module");
        const string areaResRef = "veles_exterior";
        if (!Directory.Exists(moduleRoot) || !File.Exists(Path.Combine(moduleRoot, "are", areaResRef + ".are.json")))
            throw new DirectoryNotFoundException("The selected SWLOR module must contain the veles_exterior area fixture.");

        var log = new OutputLogService();
        var workspace = new ModuleWorkspace(moduleRoot, resources);
        var lookups = new LookupOptionProvider(new WorkspaceContext(_ => throw new NotSupportedException(), log));
        var viewModel = new AreaEditorViewModel(areaResRef, workspace, lookups, null, log,
            tilesetCatalog: new TilesetCatalog(resources), tileModelCache: new TileModelCache(resources),
            resourceIndex: resources, prompts: new EditorPromptService());
        var view = new AreaEditorView { DataContext = viewModel };
        var sceneView = view.FindControl<AreaSceneView>("SceneView")
            ?? throw new InvalidOperationException("The real AreaEditorView must contain its shared SceneView.");

        var window = new Window
        {
            Title = "SWLOR Toolset - Area Editor capture",
            Width = 1200,
            Height = 900,
            MinWidth = 800,
            MinHeight = 500,
            Background = Avalonia.Media.Brushes.Black,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new Avalonia.PixelPoint(-32000, -32000),
            Content = view,
        };
        var layout = view.FindControl<AreaEditorLayout>("RootTabs")
            ?? throw new InvalidOperationException("The real AreaEditorView must contain its shared tab layout.");
        var step = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                if (step == 0 && (viewModel.AreaScene is null || !ReferenceEquals(sceneView.Viewport.Scene, viewModel.AreaScene)))
                {
                    timer.Start();
                    return;
                }

                var destination = step == 0 ? output : propertiesOutput;
                var result = NativeWindowScreenshot.Capture(window, destination);
                Console.WriteLine($"AreaEditorView {step switch { 0 => "scene", _ => "properties" }} capture: {result.Width}x{result.Height}, {result.BytesWritten} bytes, {destination}");
                if (step == 0)
                {
                    var scene = viewModel.AreaScene!;
                    Console.WriteLine($"Real module scene {areaResRef}: {scene.Width}x{scene.Height}, tiles={scene.Tiles.Count}, instances={scene.Instances.Count}, diagnostics={scene.Diagnostics.MissingModels.Count}");
                    layout.SelectedIndex = 1;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    step = 1;
                    timer.Interval = CaptureDelay;
                    timer.Start();
                    return;
                }

                desktop.Shutdown(0);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("The complete SWLOR AreaEditorView capture failed: " + exception);
                desktop.Shutdown(1);
            }
        };
        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            timer.Stop();
            Console.Error.WriteLine($"The actual SWLOR AreaEditorView scene did not become ready within sixty seconds. Status={viewModel.SceneStatus}; building={viewModel.IsBuildingScene}; scene={(viewModel.AreaScene is null ? "missing" : "ready")}." );
            desktop.Shutdown(2);
        };
        desktop.Exit += (_, _) =>
        {
            timer.Stop();
            timeout.Stop();
        };
        desktop.MainWindow = window;
        window.Opened += (_, _) =>
        {
            timer.Start();
            timeout.Start();
        };
    }
}
