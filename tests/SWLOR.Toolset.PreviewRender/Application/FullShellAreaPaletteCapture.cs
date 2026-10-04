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

namespace SWLOR.Toolset.PreviewRender.Application;

internal static class FullShellAreaPaletteCapture
{
    private static string AreaResRef => Environment.GetEnvironmentVariable("SWLOR_AREA_EDITOR_CAPTURE_AREA_RESREF") is { Length: > 0 } value ? value : "veles_exterior";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);

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
        if (acceptanceStage != "edit" && !File.Exists(Path.Combine(moduleSource, "are", AreaResRef + ".are.json")))
        {
            throw new FileNotFoundException($"The selected SWLOR module must contain the {AreaResRef} area fixture.");
        }

        if (Directory.Exists(runRoot) || File.Exists(runRoot))
        {
            throw new IOException("The full-shell capture run folder must be new and empty by construction.");
        }

        EnsureNewOutput(output);
        Directory.CreateDirectory(runRoot);
        CopyDirectoryWithoutReparsePoints(moduleSource, moduleRoot);
        File.WriteAllText(iniPath,
            "[Alias]" + Environment.NewLine +
            "HAK=" + packedHakRoot + Environment.NewLine +
            "TLK=" + packedTlkRoot + Environment.NewLine);

        VerifyPackedModuleInputs(moduleRoot, iniPath, runRoot);

        var settingsPath = Path.Combine(runRoot, "settings.json");
        var settings = ToolsetSettings.Load(settingsPath);
        settings.ModuleRoot = moduleRoot;
        settings.NwnInstallOverride = installRoot;
        settings.PaletteSelection = "utp";

        var configPath = Path.Combine(repositoryRoot, "Build", "hakbuilder.json");
        var resourceIndexWatch = System.Diagnostics.Stopwatch.StartNew();
        var resourceIndex = ResourceIndex.FromHakBuilderConfig(
            configPath,
            haksRoot,
            KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
        if (!resourceIndex.InitializationTask.Wait(Remaining(started)))
        {
            throw new TimeoutException("The real SWLOR HAK and base-game resource index exceeded the 60-second capture deadline.");
        }
        var resourceIndexMilliseconds = resourceIndexWatch.ElapsedMilliseconds;

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
                    acceptanceStage, resourceIndexMilliseconds, sceneLoadWatch);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Full-shell SWLOR Palette capture failed: " + exception);
                desktop.Shutdown(1);
            }
        };
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
        long resourceIndexMilliseconds,
        System.Diagnostics.Stopwatch sceneLoadWatch)
    {
        while (true)
        {
            EnsureWithinDeadline(started, "loading the real area editor and shared Palette");
            await Dispatcher.UIThread.InvokeAsync(() => { });
            var areaView = window.GetVisualDescendants().OfType<AreaEditorView>().FirstOrDefault();
            if (palette.PresentationState.Tiles.Count > 0 && !palette.PresentationState.Tiles[0].PreviewRequested)
                palette.PresentationState.EnsurePreview(palette.PresentationState.Tiles[0]);
            var paletteView = window.GetVisualDescendants().OfType<PaletteView>().FirstOrDefault();
            if (areaView?.DataContext is not AreaEditorViewModel viewModel
                || viewModel.IsBuildingScene
                || viewModel.AreaScene is null
                || paletteView is null
                || !paletteView.IsEffectivelyVisible
                || palette.PresentationState.Tiles.Count == 0
                || !palette.PresentationState.Tiles[0].HasPreview
                || !AreaContentsIsMounted(window, areaContents, viewModel))
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

            var sceneView = areaView.FindControl<AreaSceneView>("SceneView")
                ?? throw new InvalidOperationException("The real AreaEditorView does not contain its SceneView.");
            if (!sceneView.IsEffectivelyVisible || scene.Tiles.Count == 0)
            {
                throw new InvalidOperationException("The real SWLOR Area Editor has no visible native scene tiles.");
            }

            if (palette.PresentationState.Tiles.Count == 0
                || !palette.PresentationState.Tiles[0].HasPreview)
            {
                throw new InvalidOperationException("The mounted shared Palette did not complete a real preview callback.");
            }

            var editor = areaView.DataContext as AreaEditorViewModel
                ?? throw new InvalidOperationException("The opened area view has no production AreaEditorViewModel.");
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
            else if (acceptanceStage != "capture")
            {
                throw new InvalidOperationException("Unknown SWLOR area acceptance stage: " + acceptanceStage);
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
            desktop.Shutdown(0);
            return;
        }
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
    private static void VerifyPackedModuleInputs(string moduleRoot, string iniPath, string runRoot)
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
            : throw new TimeoutException("The full-shell capture exceeded its 60-second deadline.");
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
