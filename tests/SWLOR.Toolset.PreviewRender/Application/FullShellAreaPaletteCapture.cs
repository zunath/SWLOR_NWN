using System.Reflection;
using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SWLOR.NWN.Formats.Common;
using Nwn.Authoring.Documents.Native;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Palettes.Views;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.PreviewRender.Viewport;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;
using SWLOR.Toolset.PreviewRender.Performance;
using SharedModelPreviewControl = Nwn.Toolset.Avalonia.Viewport.ModelPreviewControl;
using ModelViewportRenderObservation = Nwn.Toolset.Avalonia.Viewport.ModelViewportRenderObservation;
using PreparedScene = Nwn.Preview.Scene.PreparedScene;

namespace SWLOR.Toolset.PreviewRender.Application;

internal static class FullShellAreaPaletteCapture
{
    private static string AreaResRef => Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_AREA_RESREF") is { Length: > 0 } value ? value : "veles_exterior";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(180);

    public static void Start(IClassicDesktopStyleApplicationLifetime desktop, App application)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var repositoryRoot = RequireDirectory("SWLOR_TEST_REPOSITORY_ROOT");
        var haksRoot = RequireDirectory("SWLOR_HAKS_ROOT");
        var packedHakRoot = RequireDirectory("SWLOR_PACKED_HAK_ROOT");
        var packedTlkRoot = RequireDirectory("SWLOR_PACKED_TLK_ROOT");
        var installRoot = RequireDirectory("SWLOR_NWN_INSTALL_ROOT");
        var artifactRoot = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_ARTIFACT_ROOT") is { Length: > 0 } artifactRootValue
            ? Path.GetFullPath(artifactRootValue)
            : Path.GetFullPath(Path.Combine(repositoryRoot, "artifacts"));
        var runRoot = Path.GetFullPath(RequireValue("SWLOR_AREA_EDITOR_CAPTURE_RUN_ROOT"));
        var output = Path.GetFullPath(RequireValue("SWLOR_AREA_EDITOR_CAPTURE_OUTPUT"));
        EnsureContainedPath(artifactRoot, runRoot);
        EnsureContainedPath(artifactRoot, output);
        EnsureNoReparsePointAncestors(artifactRoot, runRoot);
        EnsureNoReparsePointAncestors(artifactRoot, Path.GetDirectoryName(output)!);
        var moduleSource = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_MODULE_SOURCE") is { Length: > 0 } moduleSourceValue
            ? Path.GetFullPath(moduleSourceValue)
            : Path.Combine(repositoryRoot, "Module");
        if (!Directory.Exists(moduleSource)) throw new DirectoryNotFoundException("The explicit module source does not exist: " + moduleSource);
        var moduleRoot = Path.Combine(runRoot, "Module");
        var iniPath = Path.Combine(runRoot, "nwn.ini");

        if (!File.Exists(Path.Combine(repositoryRoot, "Build", "hakbuilder.json")))
        {
            throw new FileNotFoundException("The selected SWLOR checkout must contain Build/hakbuilder.json.");
        }

        var acceptanceStage = Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_STAGE") ?? "capture";
        ToolsetPerformanceObserver? performanceRun = null;
        if (acceptanceStage is not ("edit" or "performance-edit") && !File.Exists(Path.Combine(moduleSource, "are", AreaResRef + ".are.json")))
        {
            throw new FileNotFoundException($"The selected SWLOR module must contain the {AreaResRef} area fixture.");
        }

        if (Directory.Exists(runRoot) || File.Exists(runRoot))
        {
            throw new IOException("The full-shell capture run folder must be new and empty by construction.");
        }

        EnsureNewOutput(output);
        Directory.CreateDirectory(runRoot);
        if (acceptanceStage is "performance" or "performance-edit" or "performance-reopen")
            performanceRun = new ToolsetPerformanceObserver(runRoot, AreaResRef);
        CopyDirectoryWithoutReparsePoints(moduleSource, moduleRoot);
        File.WriteAllText(iniPath,
            "[Alias]" + Environment.NewLine +
            "HAK=" + packedHakRoot + Environment.NewLine +
            "TLK=" + packedTlkRoot + Environment.NewLine);

        var moduleHakLayers = VerifyPackedModuleInputs(moduleRoot, iniPath, runRoot);

        var settingsPath = Path.Combine(runRoot, "settings.json");
        var settings = ToolsetSettings.Load(settingsPath);
        settings.ModuleRoot = moduleRoot;
        settings.NwnInstallOverride = installRoot;
        settings.PaletteSelection = "utp";

        performanceRun?.SetPhase(PerformancePhase.ResourcePreparation);
        var resourceIndexWatch = System.Diagnostics.Stopwatch.StartNew();
        var resourceIndex = ResourceIndex.CreateDeferred(
            moduleHakLayers,
            () => KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
        long resourceIndexMilliseconds = 0;

        var services = new ServiceCollection();
        typeof(App).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { services, settings });
        services.RemoveAll<ResourceIndex>();
        services.RemoveAll<TwoDaService>();
        services.RemoveAll<TilesetCatalog>();
        services.RemoveAll<TileModelCache>();
        services.RemoveAll<ModuleCustomContentService>();
        services.AddSingleton(resourceIndex);
        services.AddSingleton(new TwoDaService(resourceIndex));
        services.AddSingleton(new TilesetCatalog(resourceIndex));
        services.AddSingleton(new TileModelCache(resourceIndex));

        // The feature worktree may not contain the optional HAK/TLK checkout beside the
        // executable, so bind the real shell lookup services to the explicitly selected corpus.
        var swTlkJsonPath = Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json");
        if (!File.Exists(swTlkJsonPath))
            throw new FileNotFoundException("The selected SWLOR HAK root has no sw_tlk.tlk.json.", swTlkJsonPath);

        services.RemoveAll<TlkService>();
        services.AddSingleton<TlkService>(provider => TlkService.LoadDeferredWithOptionalBase(
            swTlkJsonPath,
            ResolveBaseDialogTlk(installRoot),
            warning => provider.GetRequiredService<OutputLogService>().AppendLine(warning)));

        services.RemoveAll<AppearanceService>();
        services.AddSingleton(provider =>
            new AppearanceService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<PlaceableAppearanceService>();
        services.AddSingleton(provider =>
            new PlaceableAppearanceService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<PlaceableModelCatalog>();
        services.AddSingleton(provider =>
            new PlaceableModelCatalog(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<DoorTypeService>();
        services.AddSingleton(provider =>
            new DoorTypeService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<WaypointAppearanceService>();
        services.AddSingleton(provider =>
            new WaypointAppearanceService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<SoundService>();
        services.AddSingleton(provider =>
            new SoundService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<TwoDaLookupService>();
        services.AddSingleton(provider =>
            new TwoDaLookupService(provider.GetRequiredService<TwoDaService>(), provider.GetRequiredService<TlkService>()));
        services.RemoveAll<PortraitService>();
        services.AddSingleton(provider => new PortraitService(provider.GetRequiredService<TwoDaService>()));
        services.RemoveAll<BaseItemIconService>();
        services.AddSingleton(provider => new BaseItemIconService(provider.GetRequiredService<TwoDaService>()));
        services.AddSingleton(provider => new ModuleCustomContentService(
            provider.GetRequiredService<WorkspaceContext>(),
            provider.GetRequiredService<OutputLogService>(),
            resourceIndex,
            provider.GetService<TlkService>(),
            iniPath,
            settings));

        var provider = services.BuildServiceProvider();
        typeof(App).GetField("_serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(application, provider);

        var window = new MainWindow(settings)
        {
            Width = 1280,
            Height = 800,
            MinWidth = 1000,
            MinHeight = 650,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new Avalonia.PixelPoint(-32000, -32000)
        };
        var shell = provider.GetRequiredService<ShellViewModel>();
        var moduleCustomContent = provider.GetRequiredService<ModuleCustomContentService>();
        var contentReloaded = new TaskCompletionSource<ModuleCustomContentReloadResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        moduleCustomContent.Reloaded += result => contentReloaded.TrySetResult(result);
        window.AttachViewModel(shell);
        desktop.MainWindow = window;
        window.Opened += async (_, _) =>
        {
            try
            {
                try
                {
                    await resourceIndex.InitializationTask.WaitAsync(Remaining(started));
                }
                catch (TimeoutException exception)
                {
                    throw new TimeoutException("The real SWLOR HAK and base-game resource index exceeded the three-minute capture deadline.", exception);
                }
                resourceIndexMilliseconds = resourceIndexWatch.ElapsedMilliseconds;

                await shell.InitializeAsync();
                EnsureWithinDeadline(started, "opening the copied module");
                var contentResult = await contentReloaded.Task.WaitAsync(Remaining(started));
                if (!contentResult.ResourceIndexAvailable || contentResult.MissingHaks.Count > 0 || contentResult.LoadedHakCount != contentResult.AssignedHakCount)
                {
                    throw new InvalidDataException(
                        $"The copied module has packed HAK assignments missing from the selected packed HAK root ({contentResult.LoadedHakCount}/{contentResult.AssignedHakCount}, missing={contentResult.MissingHaks.Count}).");
                }

                var workspaceContext = provider.GetRequiredService<WorkspaceContext>();
                var catalog = workspaceContext.Catalog
                    ?? throw new InvalidOperationException("The copied SWLOR module did not produce a blueprint catalog.");
                await catalog.BuildTask.WaitAsync(Remaining(started));
                EnsureWithinDeadline(started, "indexing the copied module");

                var palette = provider.GetRequiredService<PaletteViewModel>();
                if (acceptanceStage is "edit" or "reopen")
                {
                    var presentation = palette.PresentationState;
                    presentation.ShowStandardCommand.Execute(null);
                    presentation.SelectTypeCommand.Execute(presentation.Types.Single(type => type.Option.Type is null));
                    presentation.UseManualTilePaintCommand.Execute(null);
                    if (presentation.Rows.SingleOrDefault(row => row.Name == "All tiles") is { } allTiles)
                        presentation.SelectedRow = allTiles;
                }
                if (acceptanceStage == "edit")
                {
                    var explorer = provider.GetRequiredService<ModuleExplorerViewModel>();
                    explorer.SelectedType = ResourceType.Area;
                    explorer.NewItemCommand.Execute(null);
                    var form = explorer.ActiveNewArea
                        ?? throw new InvalidOperationException("The production Module Contents did not open its New Area form.");
                    form.ResRef = AreaResRef;
                    form.DisplayName = "SW shared area acceptance";
                    form.SelectedTileset = form.Tilesets.SingleOrDefault(item => item.ResRef.Equals("ttr01", StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("The licensed native TTR01 tileset is not available in the production New Area form.");
                    form.Width = 4;
                    form.Height = 4;
                    form.CreateCommand.Execute(null);
                    var areaStem = Path.Combine(moduleRoot, "are", AreaResRef + ".are.json");
                    var gitStem = Path.Combine(moduleRoot, "git", AreaResRef + ".git.json");
                    var gicStem = Path.Combine(moduleRoot, "gic", AreaResRef + ".gic.json");
                    if (!File.Exists(areaStem) || !File.Exists(gitStem) || !File.Exists(gicStem))
                    {
                        throw new InvalidOperationException(
                            $"Production New Area did not create its full resource triplet: are={File.Exists(areaStem)}, git={File.Exists(gitStem)}, gic={File.Exists(gicStem)}, formStatus='{form.StatusMessage}', explorerStatus='{explorer.StatusMessage}'.");
                    }
                }
                else
                {
                    provider.GetRequiredService<EditorService>().TryOpenEditor(ResourceType.Area, AreaResRef);
                }
                var areaContents = provider.GetRequiredService<AreaContentsViewModel>();
                var sceneLoadWatch = System.Diagnostics.Stopwatch.StartNew();
                await CaptureWhenReadyAsync(window, desktop, palette, areaContents, workspaceContext, output, runRoot, started,
                    acceptanceStage, resourceIndex, resourceIndexMilliseconds, sceneLoadWatch, performanceRun);
            }
            catch (Exception exception)
            {
                if (performanceRun is not null)
                    await performanceRun.StopAndWriteAsync();
                Console.Error.WriteLine("Full-shell SWLOR Palette capture failed: " + exception);
                desktop.Shutdown(1);
            }
        };
    }

    private static string? ResolveBaseDialogTlk(string installRoot)
    {
        foreach (var language in new[] { "en", "fr", "de", "it", "es", "pl" })
        {
            var candidate = Path.Combine(installRoot, "lang", language, "data", "dialog.tlk");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static async Task CaptureWhenReadyAsync(
        MainWindow window,
        IClassicDesktopStyleApplicationLifetime desktop,
        PaletteViewModel palette,
        AreaContentsViewModel areaContents,
        WorkspaceContext workspaceContext,
        string sceneOutput,
        string runRoot,
        System.Diagnostics.Stopwatch started,
        string acceptanceStage,
        ResourceIndex resourceIndex,
        long resourceIndexMilliseconds,
        System.Diagnostics.Stopwatch sceneLoadWatch,
        ToolsetPerformanceObserver? performanceRun)
    {
        var lastReadiness = "The area editor has not yet been inspected.";
        while (true)
        {
            if (started.Elapsed >= Deadline)
            {
                throw new TimeoutException(
                    $"The full-shell capture exceeded its three-minute deadline while waiting for the area editor. Last readiness state: {lastReadiness}");
            }

            await Dispatcher.UIThread.InvokeAsync(() => { });
            var areaView = window.GetVisualDescendants().OfType<AreaEditorView>().FirstOrDefault();
            if (palette.PresentationState.Tiles.Count > 0 && !palette.PresentationState.Tiles[0].PreviewRequested)
                palette.PresentationState.EnsurePreview(palette.PresentationState.Tiles[0]);
            var paletteView = window.GetVisualDescendants().OfType<PaletteView>().FirstOrDefault();
            var viewModel = areaView?.DataContext as AreaEditorViewModel;
            var currentScene = viewModel?.AreaScene;
            if (currentScene is not null && !palette.PresentationState.IsTileMode)
                palette.SelectMode(Nwn.Toolset.Avalonia.Palettes.PaletteMode.Tiles);
            var firstTile = palette.PresentationState.Tiles.FirstOrDefault();
            var contentsMounted = viewModel is not null && AreaContentsIsMounted(window, areaContents, viewModel);
            lastReadiness = $"areaView={areaView is not null}, viewModel={viewModel is not null}, area={viewModel?.AreaResRef ?? "(none)"}, sceneBuilding={viewModel?.IsBuildingScene}, scene={(currentScene is null ? "missing" : $"{currentScene.Width}x{currentScene.Height},tiles={currentScene.Tiles.Count},instances={currentScene.Instances.Count}")}, paletteView={paletteView is not null}, paletteVisible={paletteView?.IsEffectivelyVisible}, tileRows={palette.PresentationState.Tiles.Count}, firstPreviewRequested={firstTile?.PreviewRequested}, firstPreviewReady={firstTile?.HasPreview}, areaContentsMounted={contentsMounted}, sceneStatus={viewModel?.SceneStatus ?? "(none)"}";
            if (viewModel is null
                || viewModel.IsBuildingScene
                || currentScene is null
                || paletteView is null
                || !paletteView.IsEffectivelyVisible
                || firstTile is null
                || ((acceptanceStage is "edit" or "reopen") && !firstTile.HasPreview)
                || !contentsMounted)
            {
                await Task.Delay(100);
                continue;
            }

            var scene = viewModel.AreaScene
                ?? throw new InvalidOperationException("The Area Editor lost its native scene during capture.");
            var expectedInstanceCount = viewModel.Sections.Sum(section => section.Rows.Count);
            VerifyAreaContentsMount(window, areaContents, viewModel, expectedInstanceCount);
            if (scene.Diagnostics.MissingModels.Count > 0)
            {
                throw new InvalidDataException(
                    $"The real SWLOR scene has {scene.Diagnostics.MissingModels.Count} missing models; first: {scene.Diagnostics.MissingModels[0]}.");
            }

            var sceneView = areaView!.FindControl<AreaSceneView>("SceneView")
                ?? throw new InvalidOperationException("The real AreaEditorView does not contain its SceneView.");
            if (!sceneView.IsEffectivelyVisible || scene.Tiles.Count == 0)
            {
                throw new InvalidOperationException("The real SWLOR Area Editor has no visible native scene tiles.");
            }

            if ((acceptanceStage is "edit" or "reopen")
                && (palette.PresentationState.Tiles.Count == 0
                    || !palette.PresentationState.Tiles[0].HasPreview))
            {
                throw new InvalidOperationException("The mounted shared Palette did not complete a real preview callback.");
            }

            var editor = areaView!.DataContext as AreaEditorViewModel
                ?? throw new InvalidOperationException("The opened area view has no production AreaEditorViewModel.");
            await CaptureCompletedAreaFramesAsync(sceneView.Viewport, scene, runRoot, started);
            if (performanceRun is not null)
            {
                if (acceptanceStage == "performance-reopen")
                {
                    performanceRun.SetPhase(PerformancePhase.Reopen);
                    performanceRun.Record(PerformancePhase.Reopen, PerformanceOperation.FreshWorkspaceReopen,
                        sceneLoadWatch, "Area load through twenty completed current-scene viewport frames.");
                }
                performanceRun.WriteReady(editor.AreaResRef, scene.Width, scene.Height, scene.Tiles.Count, scene.Instances.Count, 20);
                await MeasurePaletteInteractionsAsync(window, palette, performanceRun, started);
                await MeasureRenderedModelSelectionsAsync(window, scene, resourceIndex, performanceRun, started);
            }
            long? firstQualifiedWindowFrameMilliseconds = null;
            if (acceptanceStage is "edit" or "reopen")
            {
                var firstFramePath = Path.Combine(runRoot, "first-area-window-frame.png");
                var firstFrameWatch = System.Diagnostics.Stopwatch.StartNew();
                var firstFrame = NativeWindowScreenshot.Capture(window, firstFramePath);
                firstQualifiedWindowFrameMilliseconds = started.ElapsedMilliseconds;
                Console.WriteLine($"First qualified rendered-area window frame: {firstFrame.Width}x{firstFrame.Height}, {firstFrame.BytesWritten} bytes, elapsed={firstQualifiedWindowFrameMilliseconds}ms, captureCall={firstFrameWatch.ElapsedMilliseconds}ms.");
            }
            if (acceptanceStage == "edit")
            {
                await SharedAreaNativeAcceptance.RunAsync(window, editor, palette, workspaceContext, Path.Combine(runRoot, "Module"), runRoot,
                    resourceIndexMilliseconds, sceneLoadWatch.ElapsedMilliseconds, firstQualifiedWindowFrameMilliseconds);
            }
            else if (acceptanceStage == "reopen")
            {
                await SharedAreaNativeAcceptance.VerifyReopenAsync(editor, Path.Combine(runRoot, "Module"), runRoot,
                    resourceIndexMilliseconds, sceneLoadWatch.ElapsedMilliseconds, firstQualifiedWindowFrameMilliseconds);
            }
            else if (acceptanceStage is "performance-edit")
            {
                await AreaPerformanceAcceptance.EditSaveAndPackAsync(editor, Path.Combine(runRoot, "Module"), runRoot, performanceRun!);
            }
            else if (acceptanceStage is "performance-reopen")
            {
                await AreaPerformanceAcceptance.VerifyFreshReopenAsync(editor, Path.Combine(runRoot, "Module"), runRoot);
            }
            else if (acceptanceStage is not ("capture" or "performance"))
            {
                throw new InvalidOperationException("Unknown SWLOR area acceptance stage: " + acceptanceStage);
            }

            if (performanceRun is not null)
            {
                await performanceRun.SetPhaseAndDrainAsync(PerformancePhase.Capture);
                var firstFramePath = Path.Combine(runRoot, "first-area-window-frame.png");
                var firstFrameWatch = System.Diagnostics.Stopwatch.StartNew();
                var firstFrame = NativeWindowScreenshot.Capture(window, firstFramePath);
                Console.WriteLine($"Post-operation rendered-area window frame: {firstFrame.Width}x{firstFrame.Height}, {firstFrame.BytesWritten} bytes, elapsed={started.ElapsedMilliseconds}ms, captureCall={firstFrameWatch.ElapsedMilliseconds}ms.");
            }
            WriteLoadedAssemblyEvidence(runRoot);
            var sceneResult = NativeWindowScreenshot.Capture(window, sceneOutput);
            Console.WriteLine(
                $"Full SWLOR shell scene capture: hwnd=0x{sceneResult.Hwnd.ToInt64():X}, {sceneResult.Width}x{sceneResult.Height}, {sceneResult.BytesWritten} bytes, {sceneOutput}");
            Console.WriteLine(
                $"Area {AreaResRef}: {scene.Width}x{scene.Height}, tiles={scene.Tiles.Count}, instances={scene.Instances.Count}, missingModels={scene.Diagnostics.MissingModels.Count}; paletteEntries={palette.PresentationState.Tiles.Count}; previewRequested={palette.PresentationState.Tiles[0].PreviewRequested}, previewCallbackImage={palette.PresentationState.Tiles[0].HasPreview}; modelPixelsNotQualified=true");

            var paletteSourceSwitch = paletteView.GetVisualDescendants().OfType<StackPanel>()
                .Single(panel => panel.Children.OfType<Button>()
                    .Any(button => button.Content?.ToString() == palette.PresentationState.Texts.CustomSource));
            var tilePaintSwitch = paletteView.GetVisualDescendants().OfType<StackPanel>()
                .Single(panel => panel.Children.OfType<Button>()
                    .Any(button => button.Content?.ToString() == palette.PresentationState.Texts.AutoTilePaint));
            var autoPaintButton = tilePaintSwitch.Children.OfType<Button>()
                .Single(button => button.Content?.ToString() == palette.PresentationState.Texts.AutoTilePaint);
            var manualPaintButton = tilePaintSwitch.Children.OfType<Button>()
                .Single(button => button.Content?.ToString() == palette.PresentationState.Texts.ManualTilePaint);
            palette.SelectMode(Nwn.Toolset.Avalonia.Palettes.PaletteMode.Tiles);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            await Task.Delay(200);
            await Dispatcher.UIThread.InvokeAsync(() => { });
            window.UpdateLayout();
            var tileState = palette.PresentationState;
            if (tileState.Tiles.Count == 0)
            {
                throw new InvalidOperationException("The real shared Palette has no Tiles-mode entries for comparison.");
            }

            if (!tileState.IsTileMode || tileState.ShowsSourceSwitch || !tileState.ShowsTilePaintSwitch
                || paletteSourceSwitch.IsVisible || !tilePaintSwitch.IsVisible)
            {
                throw new InvalidOperationException("The mounted Palette controls do not match Tiles mode.");
            }

            if (tileState.Tiles.FirstOrDefault() is { } initialTileRow)
            {
                tileState.EnsurePreview(initialTileRow);
            }

            if (tileState.ShowsTilePaintSwitch)
            {
                foreach (var paintMode in new[]
                {
                    Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Auto,
                    Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Manual
                })
                {
                    EnsureWithinDeadline(started, $"capturing the Tiles {paintMode} mode");
                    palette.SelectTilePaintMode(paintMode);
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    await Task.Delay(200);
                    await Dispatcher.UIThread.InvokeAsync(() => { });
                    window.UpdateLayout();
                    if (tileState.TilePaintMode != paintMode
                        || !tilePaintSwitch.IsVisible
                        || paletteSourceSwitch.IsVisible
                        || tileState.IsAutoTilePaint != (paintMode == Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Auto)
                        || tileState.IsManualTilePaint != (paintMode == Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Manual)
                        || autoPaintButton.Classes.Contains("primary") != (paintMode == Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Auto)
                        || manualPaintButton.Classes.Contains("primary") != (paintMode == Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Manual))
                    {
                        throw new InvalidOperationException($"The mounted switch controls do not reflect {paintMode} tile-paint mode.");
                    }

                    if (tileState.Tiles.FirstOrDefault() is { } currentTileRow)
                    {
                        tileState.EnsurePreview(currentTileRow);
                    }

                    var modeName = paintMode.ToString().ToLowerInvariant();
                    var tilesOutput = Path.Combine(
                        Path.GetDirectoryName(sceneOutput)!,
                        Path.GetFileNameWithoutExtension(sceneOutput) + $"-tiles-{modeName}" + Path.GetExtension(sceneOutput));
                    EnsureNewOutput(tilesOutput);
                    var tilesResult = NativeWindowScreenshot.Capture(window, tilesOutput);
                    Console.WriteLine(
                        $"Full SWLOR shell Tiles {paintMode} capture: hwnd=0x{tilesResult.Hwnd.ToInt64():X}, {tilesResult.Width}x{tilesResult.Height}, {tilesResult.BytesWritten} bytes, previewCallbackImage={tileState.Tiles.FirstOrDefault()?.HasPreview == true}; modelPixelsNotQualified=true, {tilesOutput}");
                }
            }
            else
            {
                var tilesOutput = Path.Combine(
                    Path.GetDirectoryName(sceneOutput)!,
                    Path.GetFileNameWithoutExtension(sceneOutput) + "-tiles-unsupported" + Path.GetExtension(sceneOutput));
                EnsureNewOutput(tilesOutput);
                var tilesResult = NativeWindowScreenshot.Capture(window, tilesOutput);
                Console.WriteLine(
                    $"Full SWLOR shell Tiles capture: hwnd=0x{tilesResult.Hwnd.ToInt64():X}, {tilesResult.Width}x{tilesResult.Height}, {tilesResult.BytesWritten} bytes, tilePaintSwitchSupported=false, previewCallbackImage={tileState.Tiles.FirstOrDefault()?.HasPreview == true}; modelPixelsNotQualified=true, {tilesOutput}");
            }
            var layout = areaView.FindControl<AreaEditorLayout>("RootTabs")
                ?? throw new InvalidOperationException("The real AreaEditorView does not contain its native tab layout.");
            layout.SelectedIndex = 1;
            window.UpdateLayout();
            await Task.Delay(500);
            EnsureWithinDeadline(started, "capturing the properties view");

            var propertiesOutput = Path.Combine(
                Path.GetDirectoryName(sceneOutput)!,
                Path.GetFileNameWithoutExtension(sceneOutput) + "-properties" + Path.GetExtension(sceneOutput));
            EnsureNewOutput(propertiesOutput);
            var propertiesResult = NativeWindowScreenshot.Capture(window, propertiesOutput);
            Console.WriteLine(
                $"Full SWLOR shell properties capture: hwnd=0x{propertiesResult.Hwnd.ToInt64():X}, {propertiesResult.Width}x{propertiesResult.Height}, {propertiesResult.BytesWritten} bytes, {propertiesOutput}");
            if (performanceRun is not null)
                await performanceRun.StopAndWriteAsync();
            desktop.Shutdown(0);
            return;
        }
    }

    private static async Task MeasurePaletteInteractionsAsync(
        Window window,
        PaletteViewModel palette,
        ToolsetPerformanceObserver observer,
        System.Diagnostics.Stopwatch started)
    {
        var state = palette.PresentationState;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            state.Query = string.Empty;
            state.UseManualTilePaintCommand.Execute(null);
            window.UpdateLayout();
        });
        await WaitForAsync(
            () => state.TilePaintMode == Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode.Manual
                && state.Rows.Any(row => row.Name == Nwn.Authoring.Areas.Tiles.TilePaletteBuilder.AllTilesCategoryName && row.Count > 0),
            "the production Manual tile mode and its All tiles category");
        var allTilesCategory = state.Rows.SingleOrDefault(row => row.Name == Nwn.Authoring.Areas.Tiles.TilePaletteBuilder.AllTilesCategoryName && row.Count > 0)
            ?? throw new InvalidDataException("The real tile palette has no populated All tiles category for performance sampling.");
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            state.Query = string.Empty;
            state.SelectedRow = allTilesCategory;
            window.UpdateLayout();
        });
        await WaitForAsync(
            () => state.SelectedRow?.Id == allTilesCategory.Id && state.Tiles.Count == Math.Min(allTilesCategory.Count, Nwn.Toolset.Avalonia.Palettes.PalettePresentationState.MaxSearchResults),
            "the real All tiles palette category projection");
        if (state.SelectedRow?.Id != allTilesCategory.Id)
            throw new InvalidDataException("The mounted shared palette did not retain the explicitly selected All tiles category.");

        var supported = state.Tiles.Where(row => row.Snapshot.SupportsPreview).ToArray();
        var first = supported.FirstOrDefault(row => !row.PreviewRequested && !row.HasPreview)
            ?? throw new InvalidDataException("The real palette has no unrequested preview-capable tile for a first-thumbnail observation.");
        var cachedRows = supported.Where(row => !ReferenceEquals(row, first)).Take(20).ToArray();
        if (cachedRows.Length < 20)
            throw new InvalidDataException($"The selected real category {allTilesCategory.Name} exposes only {cachedRows.Length} additional preview-capable rows from {supported.Length} total; 20 are required for cached-selection sampling.");
        if (cachedRows.Select(row => row.Snapshot.Id).Distinct().Count() != 20)
            throw new InvalidDataException("The selected real category did not provide 20 distinct cached tile rows.");

        var firstWatch = System.Diagnostics.Stopwatch.StartNew();
        state.SelectedTile = first;
        state.EnsurePreview(first);
        try
        {
            await WaitForAsync(() => first.HasPreview || started.Elapsed >= Deadline,
                "the first supported real palette thumbnail");
            if (!first.HasPreview)
                throw new TimeoutException($"The first palette thumbnail did not complete. resref={first.ResRef}, requested={first.PreviewRequested}.");
            window.UpdateLayout();
            await Dispatcher.UIThread.InvokeAsync(() => { });
        }
        catch
        {
            observer.Record(PerformancePhase.Interactive, PerformanceOperation.FirstSupportedPaletteThumbnail,
                firstWatch, $"Palette thumbnail callback failed for resref={first.ResRef}.", completed: false);
            throw;
        }
        observer.Record(PerformancePhase.Interactive, PerformanceOperation.FirstSupportedPaletteThumbnail,
            firstWatch, $"Palette thumbnail callback bitmap and mounted tile row for resref={first.ResRef}; not a separate model viewport.");

        foreach (var row in cachedRows)
        {
            if (row.HasPreview)
                continue;
            state.EnsurePreview(row);
            await WaitForAsync(() => row.HasPreview || started.Elapsed >= Deadline,
                $"the cached-thumbnail preparation for {row.ResRef}");
            if (!row.HasPreview)
                throw new TimeoutException($"A preview-capable tile failed before cached-selection sampling. resref={row.ResRef}.");
        }

        for (var index = 0; index < cachedRows.Length; index++)
        {
            var row = cachedRows[index];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            state.SelectedTile = row;
            window.UpdateLayout();
            await Dispatcher.UIThread.InvokeAsync(() => { });
            if (!ReferenceEquals(state.SelectedTile, row) || !row.HasPreview || state.SelectedRow?.Id != allTilesCategory.Id)
            {
                observer.Record(PerformancePhase.Interactive, PerformanceOperation.CachedPaletteThumbnailSelection,
                    watch, $"Cached selection failed for resref={row.ResRef}.", completed: false);
                throw new InvalidDataException($"Cached palette selection {index + 1} did not retain its real bitmap row.");
            }
            observer.Record(PerformancePhase.Interactive, PerformanceOperation.CachedPaletteThumbnailSelection,
                watch, $"Cached row selection plus layout, resref={row.ResRef}; not model-viewport frame presentation.");
        }

        // Tile-mode search intentionally matches localized/display names; unlike blueprint mode,
        // it does not expose ResRefs as searchable text. Exercise the production query contract.
        var searchQueries = supported.Select(row => row.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        if (searchQueries.Length != 20)
            throw new InvalidDataException($"The real tile palette exposes only {searchQueries.Length} distinct display names for the twenty-query search sample.");
        foreach (var query in searchQueries)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            state.Query = query;
            await Dispatcher.UIThread.InvokeAsync(() => { });
            window.UpdateLayout();
            var hasVisibleMatch = state.Tiles.Any(item => string.Equals(item.Name, query, StringComparison.OrdinalIgnoreCase));
            if (!hasVisibleMatch || state.SelectedRow?.Id != allTilesCategory.Id)
            {
                observer.Record(PerformancePhase.Interactive, PerformanceOperation.PaletteSearch, watch,
                    $"Tile query {query} failed to match while retaining category {allTilesCategory.Name}.", completed: false);
                throw new InvalidDataException($"Palette search for {query} did not show its row under the selected All tiles category.");
            }
            observer.Record(PerformancePhase.Interactive, PerformanceOperation.PaletteSearch,
                watch, $"Tile palette category={allTilesCategory.Name}, display-name query={query}, visible rows={state.Tiles.Count}.");
        }
        state.Query = string.Empty;
        await Dispatcher.UIThread.InvokeAsync(() => { });
        window.UpdateLayout();
        if (state.SelectedRow?.Id != allTilesCategory.Id)
            throw new InvalidDataException("Clearing tile search did not preserve the selected All tiles category.");
    }

    private static async Task MeasureRenderedModelSelectionsAsync(
        Window owner,
        Nwn.Preview.Areas.AreaScene areaScene,
        ResourceIndex resourceIndex,
        ToolsetPerformanceObserver observer,
        System.Diagnostics.Stopwatch started)
    {
        var requestedModel = areaScene.Instances
            .Where(instance => instance.Model is not null)
            .Select(instance => instance.Model!.Name)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)
                && !string.Equals(name, "pfa0_chest001", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("The selected area has no model-backed instance distinct from the production preview window's initial model.");

        var previewWindow = new SWLOR.Toolset.Viewport.NativeModelPreviewWindow(
            new SWLOR.Toolset.Viewport.NativeModelPreviewAdapter(resourceIndex));
        // The outer capture window is positioned off the desktop for deterministic full-window
        // capture; keep this real native preview on-screen so its OpenGL frames use a visible top-level.
        previewWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        previewWindow.Opened += (_, _) => opened.TrySetResult();
        var dialogTask = previewWindow.ShowDialog(owner);
        await opened.Task.WaitAsync(Remaining(started));

        var viewportField = typeof(SWLOR.Toolset.Viewport.NativeModelPreviewWindow)
            .GetField("_viewport", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SWLOR.Toolset.Viewport.NativeModelPreviewWindow).FullName, "_viewport");
        var viewport = (SharedModelPreviewControl)(viewportField.GetValue(previewWindow)
            ?? throw new InvalidOperationException("The production Native Model Preview window has no shared model viewport."));
        var modelNameField = typeof(SWLOR.Toolset.Viewport.NativeModelPreviewWindow)
            .GetField("_resRefBox", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SWLOR.Toolset.Viewport.NativeModelPreviewWindow).FullName, "_resRefBox");
        var modelName = (TextBox)(modelNameField.GetValue(previewWindow)
            ?? throw new InvalidOperationException("The production Native Model Preview window has no model resref input."));
        var loadButton = previewWindow.GetVisualDescendants().OfType<Button>()
            .SingleOrDefault(button => string.Equals(button.Content?.ToString(), "Load", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("The production Native Model Preview window has no unique Load button.");

        try
        {
            // The real window loads its own initial model on Opened. Wait for its exact completed
            // viewport frame before timing the fixture model, so this warm-up cannot satisfy it.
            var initial = await WaitForCurrentModelFrameAsync(viewport, started);
            modelName.Text = requestedModel;
            var firstWatch = System.Diagnostics.Stopwatch.StartNew();
            loadButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            ModelViewportRenderObservation firstObservation;
            try
            {
                firstObservation = await WaitForNextModelSelectionAsync(
                    viewport, initial.Scene, initial.FrameNumber, requireNewScene: true, started);
            }
            catch
            {
                observer.Record(PerformancePhase.Interactive, PerformanceOperation.FirstRenderedModelSelection,
                    firstWatch, $"Production Native Model Preview Load click for {requestedModel}; completed frame missing.", completed: false);
                throw;
            }
            observer.RecordModelViewportIdentity(firstObservation.OpenGlVendor, firstObservation.OpenGlRenderer, firstObservation.OpenGlVersion);
            observer.Record(PerformancePhase.Interactive, PerformanceOperation.FirstRenderedModelSelection, firstWatch,
                $"Production Native Model Preview model={requestedModel}, frame={firstObservation.FrameNumber}, OpenGL={firstObservation.OpenGlVendor} | {firstObservation.OpenGlRenderer} | {firstObservation.OpenGlVersion}.");

            var previous = firstObservation;
            var frames = new HashSet<long> { firstObservation.FrameNumber };
            for (var index = 0; index < 20; index++)
            {
                var priorScene = viewport.Scene ?? throw new InvalidOperationException("The production model viewport lost its selected scene.");
                var watch = System.Diagnostics.Stopwatch.StartNew();
                loadButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                ModelViewportRenderObservation observation;
                try
                {
                    observation = await WaitForNextModelSelectionAsync(
                        viewport, priorScene, previous.FrameNumber, requireNewScene: false, started);
                }
                catch
                {
                    observer.Record(PerformancePhase.Interactive, PerformanceOperation.CachedRenderedModelSelection,
                        watch, $"Production Native Model Preview cached selection {index + 1}/20 for {requestedModel}; completed frame missing.", completed: false);
                    throw;
                }

                if (!ReferenceEquals(firstObservation.Scene, observation.Scene)
                    || observation.Textures.Count != firstObservation.Textures.Count
                    || firstObservation.Textures.Any(entry => !observation.Textures.TryGetValue(entry.Key, out var image)
                        || !ReferenceEquals(entry.Value, image)))
                    throw new InvalidDataException("A cached production model selection did not reuse the prepared model scene and decoded texture images.");
                if (!frames.Add(observation.FrameNumber))
                    throw new InvalidDataException("A repeated production model selection did not produce a distinct completed viewport frame.");
                observer.RecordModelViewportIdentity(observation.OpenGlVendor, observation.OpenGlRenderer, observation.OpenGlVersion);
                observer.Record(PerformancePhase.Interactive, PerformanceOperation.CachedRenderedModelSelection, watch,
                    $"Production Native Model Preview cached selection {index + 1}/20, model={requestedModel}, frame={observation.FrameNumber}.");
                previous = observation;
            }

            if (frames.Count != 21)
                throw new InvalidDataException("The real model viewport did not complete the first selection and all 20 distinct cached frames.");
        }
        finally
        {
            previewWindow.Close();
            await dialogTask;
        }
    }

    private static async Task<ModelViewportRenderObservation> WaitForCurrentModelFrameAsync(
        SharedModelPreviewControl viewport,
        System.Diagnostics.Stopwatch started)
    {
        while (started.Elapsed < Deadline)
        {
            var scene = viewport.Scene;
            var textures = viewport.Textures;
            var observation = viewport.LastSuccessfulRenderObservation;
            if (scene is not null && observation is not null
                && ReferenceEquals(observation.Scene, scene)
                && ReferenceEquals(observation.Textures, textures))
                return observation;
            await Task.Delay(20);
        }
        throw new TimeoutException("The production Native Model Preview initial model did not produce an exact-scene OpenGL frame before the full-shell deadline.");
    }

    private static async Task<ModelViewportRenderObservation> WaitForNextModelSelectionAsync(
        SharedModelPreviewControl viewport,
        PreparedScene previousScene,
        long previousFrameNumber,
        bool requireNewScene,
        System.Diagnostics.Stopwatch started)
    {
        while (started.Elapsed < Deadline)
        {
            var scene = viewport.Scene;
            var textures = viewport.Textures;
            var observation = viewport.LastSuccessfulRenderObservation;
            if (scene is not null
                && (requireNewScene
                    ? !ReferenceEquals(scene, previousScene)
                    : ReferenceEquals(scene, previousScene))
                && observation is not null
                && ReferenceEquals(observation.Scene, scene)
                && ReferenceEquals(observation.Textures, textures)
                && observation.FrameNumber > previousFrameNumber)
                return observation;
            await Task.Delay(20);
        }
        throw new TimeoutException("The production Native Model Preview selection did not finish with a newer exact-scene OpenGL frame before the full-shell deadline.");
    }

    private static async Task WaitForAsync(Func<bool> condition, string operation)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (deadline.Elapsed >= TimeSpan.FromSeconds(30))
                throw new TimeoutException($"Timed out waiting for {operation} after {deadline.Elapsed}.");
            await Task.Delay(25);
        }
    }

    private static async Task CaptureCompletedAreaFramesAsync(
        AreaViewportControl viewport,
        Nwn.Preview.Areas.AreaScene scene,
        string runRoot,
        System.Diagnostics.Stopwatch started)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        var observations = new List<AreaViewportRenderObservation>(20);
        long? sceneVersion = null;
        long previousFrameNumber = -1;
        while (observations.Count < 20 && deadline.Elapsed < TimeSpan.FromSeconds(15))
        {
            if (viewport.LastSuccessfulRenderObservation is { } observation
                && ReferenceEquals(observation.Scene, scene)
                && observation.FrameNumber > previousFrameNumber
                && (sceneVersion is null || sceneVersion == observation.SceneVersion))
            {
                sceneVersion ??= observation.SceneVersion;
                previousFrameNumber = observation.FrameNumber;
                observations.Add(observation);
            }

            if (observations.Count < 20)
            {
                await Task.Delay(25);
            }
        }

        if (observations.Count != 20)
        {
            throw new TimeoutException(
                $"The actual SWLOR area viewport produced {observations.Count}/20 completed frames for the current scene within 15 seconds; currentScene={ReferenceEquals(viewport.Scene, scene)}, lastFrame={previousFrameNumber}.");
        }

        var graphics = observations[^1];
        if (string.IsNullOrWhiteSpace(graphics.OpenGlVendor)
            || string.IsNullOrWhiteSpace(graphics.OpenGlRenderer)
            || string.IsNullOrWhiteSpace(graphics.OpenGlVersion))
        {
            throw new InvalidDataException("The completed SWLOR area frames did not report an active OpenGL identity.");
        }

        var evidencePath = Path.Combine(runRoot, "completed-area-scene-frames.json");
        if (File.Exists(evidencePath))
        {
            throw new IOException($"Completed-frame evidence already exists: {evidencePath}");
        }

        var evidence = new
        {
            AreaResRef,
            SceneVersion = sceneVersion,
            SceneFrameCount = observations.Count,
            FrameNumbers = observations.Select(item => item.FrameNumber).ToArray(),
            FirstFrameElapsedMilliseconds = started.ElapsedMilliseconds,
            OpenGlVendor = graphics.OpenGlVendor,
            OpenGlRenderer = graphics.OpenGlRenderer,
            OpenGlVersion = graphics.OpenGlVersion,
            CorrectnessOnly = true,
            PerformanceBudgetQualified = false
        };
        File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(
            evidence,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(
            $"Actual SWLOR AreaViewportControl completed 20 current-scene frames: sceneVersion={sceneVersion}, frames={observations[0].FrameNumber}-{observations[^1].FrameNumber}, GL={graphics.OpenGlVendor}/{graphics.OpenGlRenderer}/{graphics.OpenGlVersion}, correctnessOnly=true, evidence={evidencePath}.");
    }
    private static void WriteLoadedAssemblyEvidence(string runRoot)
    {
        var requiredAssemblies = new[] { "Nwn.Authoring", "Nwn.Formats", "Nwn.Preview", "Nwn.Toolset.Avalonia" };
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .ToArray();
        var evidence = requiredAssemblies.Select(name =>
        {
            var assembly = loadedAssemblies.FirstOrDefault(candidate =>
                string.Equals(candidate.GetName().Name, name, StringComparison.Ordinal));
            if (assembly is null)
            {
                throw new InvalidOperationException($"The full-shell capture did not load required package assembly '{name}'.");
            }

            var path = assembly.Location;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException($"The loaded package assembly '{name}' has no readable file path.", path);
            }

            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return new
            {
                Name = name,
                AssemblyVersion = assembly.GetName().Version?.ToString(),
                InformationalVersion = informationalVersion,
                Path = Path.GetFullPath(path),
                Sha256 = HashFile(path)
            };
        }).ToArray();
        var evidencePath = Path.Combine(runRoot, "loaded-package-assemblies.json");
        if (File.Exists(evidencePath))
        {
            throw new IOException($"Loaded package assembly evidence already exists: {evidencePath}");
        }

        File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(
            evidence,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Verified loaded Preview/UI assembly SHA256 evidence: {evidencePath}");
    }
    private static IReadOnlyList<ResourceIndex.HakLayer> VerifyPackedModuleInputs(string moduleRoot, string iniPath, string runRoot)
    {
        var moduleIfo = IfoDocument.Load(Path.Combine(moduleRoot, "ifo", "module.ifo.json"));
        var profile = NwnIniProfile.Load(iniPath);
        var resolution = profile.ResolveHakLayers(moduleIfo.HakNames);
        if (resolution.MissingHakNames.Count > 0)
        {
            throw new FileNotFoundException(
                $"The selected packed HAK root is missing {resolution.MissingHakNames.Count} of {moduleIfo.HakNames.Count} module HAKs: {string.Join(", ", resolution.MissingHakNames.Take(12))}.");
        }

        var haks = resolution.Layers.Select(layer => new
        {
            layer.Name,
            layer.DirectoryPath,
            Length = new FileInfo(layer.DirectoryPath).Length,
            Sha256 = HashFile(layer.DirectoryPath)
        }).ToArray();
        string? tlkPath = null;
        string? tlkHash = null;
        if (!string.IsNullOrWhiteSpace(moduleIfo.CustomTlk))
        {
            tlkPath = profile.FindTlkPath(moduleIfo.CustomTlk)
                ?? throw new FileNotFoundException($"The selected packed TLK root is missing '{moduleIfo.CustomTlk}.tlk'.");
            tlkHash = HashFile(tlkPath);
        }

        var evidencePath = Path.Combine(runRoot, "packed-module-inputs.json");
        if (File.Exists(evidencePath))
        {
            throw new IOException($"Packed module input evidence already exists: {evidencePath}");
        }

        var evidence = new
        {
            ModuleHakCount = moduleIfo.HakNames.Count,
            ResolvedHakCount = haks.Length,
            MissingHakCount = resolution.MissingHakNames.Count,
            Haks = haks,
            CustomTlk = moduleIfo.CustomTlk,
            TlkPath = tlkPath,
            TlkSha256 = tlkHash
        };
        File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(
            evidence,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(
            $"Verified packed module inputs: haks={haks.Length}/{moduleIfo.HakNames.Count}, missing=0, customTlk={moduleIfo.CustomTlk ?? "(none)"}, evidence={evidencePath}");
        return resolution.Layers;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
    private static void CopyDirectoryWithoutReparsePoints(string source, string destination)
    {
        var sourceInfo = new DirectoryInfo(Path.GetFullPath(source));
        if ((sourceInfo.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("The selected source Module is a reparse point.");
        }

        Directory.CreateDirectory(destination);
        foreach (var directory in sourceInfo.EnumerateDirectories())
        {
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException($"The selected Module contains a reparse-point directory: {directory.Name}.");
            }

            CopyDirectoryWithoutReparsePoints(directory.FullName, Path.Combine(destination, directory.Name));
        }

        foreach (var file in sourceInfo.EnumerateFiles())
        {
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException($"The selected Module contains a reparse-point file: {file.Name}.");
            }

            file.CopyTo(Path.Combine(destination, file.Name));
        }
    }

    private static void EnsureContainedPath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        if (relative == "." || relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new IOException("Full-shell capture writes must stay under the selected checkout's artifacts folder.");
        }
    }

    private static void EnsureNoReparsePointAncestors(string root, string path)
    {
        var rootInfo = new DirectoryInfo(root);
        if ((rootInfo.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("The selected checkout's artifacts folder is a reparse point.");
        }

        var current = new DirectoryInfo(path);
        while (true)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException($"The capture path contains a reparse-point directory: {current.FullName}.");
            }

            if (string.Equals(current.FullName, rootInfo.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            current = current.Parent
                ?? throw new IOException("The capture path does not descend from the selected artifacts folder.");
        }
    }

    private static void EnsureNewOutput(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            throw new IOException($"Capture output already exists: {path}");
        }

        var parent = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Capture output has no parent directory.");
        Directory.CreateDirectory(parent);
    }

    private static string RequireDirectory(string variable)
    {
        var value = RequireValue(variable);
        var path = Path.GetFullPath(value);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"{variable} does not identify an existing directory.");
        }

        return path;
    }

    private static string RequireValue(string variable) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Set {variable} to the approved local capture input.");

    private static TimeSpan Remaining(System.Diagnostics.Stopwatch started)
    {
        var remaining = Deadline - started.Elapsed;
        return remaining > TimeSpan.Zero
            ? remaining
            : throw new TimeoutException("The full-shell capture exceeded its three-minute deadline.");
    }

    private static bool AreaContentsIsMounted(
        MainWindow window,
        AreaContentsViewModel hostModel,
        AreaEditorViewModel editor)
    {
        var hostView = window.GetVisualDescendants()
            .OfType<SWLOR.Toolset.Shell.Views.AreaContentsView>()
            .FirstOrDefault();
        var sharedView = window.GetVisualDescendants()
            .OfType<Nwn.Toolset.Avalonia.Areas.Contents.Views.AreaContentsView>()
            .FirstOrDefault();
        return hostModel.HasArea
            && string.Equals(hostModel.Contents.AreaResRef, editor.AreaResRef, StringComparison.OrdinalIgnoreCase)
            && hostModel.Contents.HasArea
            && hostModel.Contents.Rows.Count > 0
            && hostView?.DataContext == hostModel
            && sharedView?.Contents == hostModel.Contents
            && sharedView.DataContext == hostModel.Contents;
    }

    private static void VerifyAreaContentsMount(
        MainWindow window,
        AreaContentsViewModel hostModel,
        AreaEditorViewModel editor,
        int expectedInstanceCount)
    {
        var hostView = window.GetVisualDescendants()
            .OfType<SWLOR.Toolset.Shell.Views.AreaContentsView>()
            .FirstOrDefault();
        var sharedView = window.GetVisualDescendants()
            .OfType<Nwn.Toolset.Avalonia.Areas.Contents.Views.AreaContentsView>()
            .FirstOrDefault();
        var actualArea = hostModel.Contents.AreaResRef;
        var actualRows = hostModel.Contents.Rows.Count;
        if (!hostModel.HasArea
            || !hostModel.Contents.HasArea
            || !string.Equals(actualArea, editor.AreaResRef, StringComparison.OrdinalIgnoreCase)
            || actualRows == 0
            || hostView?.DataContext != hostModel
            || sharedView?.Contents != hostModel.Contents
            || sharedView.DataContext != hostModel.Contents)
        {
            throw new InvalidDataException(
                $"The mounted SWLOR Area Contents wrapper is not bound to its populated host model: hostHasArea={hostModel.HasArea}, hostArea={actualArea}, editorArea={editor.AreaResRef}, rootRows={actualRows}, expectedInstances={expectedInstanceCount}, hostDataContextMatches={hostView?.DataContext == hostModel}, sharedContentsMatches={sharedView?.Contents == hostModel.Contents}, sharedDataContextMatches={sharedView?.DataContext == hostModel.Contents}.");
        }

        Console.WriteLine(
            $"Area Contents wrapper verified: area={actualArea}, sections={editor.Sections.Count}, instances={expectedInstanceCount}, rootRows={actualRows}, hostHasArea={hostModel.HasArea}, sharedHasArea={hostModel.Contents.HasArea}.");
    }

    private static void EnsureWithinDeadline(System.Diagnostics.Stopwatch started, string operation) =>
        _ = Remaining(started).Ticks > 0
            ? true
            : throw new TimeoutException($"Timed out while {operation}.");
}
