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
    private const string AreaResRef = "veles_exterior";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);

    public static void Start(IClassicDesktopStyleApplicationLifetime desktop, App application)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var repositoryRoot = RequireDirectory("SWLOR_TEST_REPOSITORY_ROOT");
        var haksRoot = RequireDirectory("SWLOR_HAKS_ROOT");
        var packedHakRoot = RequireDirectory("SWLOR_PACKED_HAK_ROOT");
        var packedTlkRoot = RequireDirectory("SWLOR_PACKED_TLK_ROOT");
        var installRoot = RequireDirectory("SWLOR_NWN_INSTALL_ROOT");
        var artifactRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "artifacts"));
        var runRoot = Path.GetFullPath(RequireValue("SWLOR_AREA_EDITOR_CAPTURE_RUN_ROOT"));
        var output = Path.GetFullPath(RequireValue("SWLOR_AREA_EDITOR_CAPTURE_OUTPUT"));
        EnsureContainedPath(artifactRoot, runRoot);
        EnsureContainedPath(artifactRoot, output);
        EnsureNoReparsePointAncestors(artifactRoot, runRoot);
        EnsureNoReparsePointAncestors(artifactRoot, Path.GetDirectoryName(output)!);
        var moduleSource = Path.Combine(repositoryRoot, "Module");
        var moduleRoot = Path.Combine(runRoot, "Module");
        var iniPath = Path.Combine(runRoot, "nwn.ini");

        if (!File.Exists(Path.Combine(repositoryRoot, "Build", "hakbuilder.json")))
        {
            throw new FileNotFoundException("The selected SWLOR checkout must contain Build/hakbuilder.json.");
        }

        if (!File.Exists(Path.Combine(moduleSource, "are", AreaResRef + ".are.json")))
        {
            throw new FileNotFoundException("The selected SWLOR module must contain the veles_exterior area fixture.");
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
        var resourceIndex = ResourceIndex.FromHakBuilderConfig(
            configPath,
            haksRoot,
            KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
        if (!resourceIndex.InitializationTask.Wait(Remaining(started)))
        {
            throw new TimeoutException("The real SWLOR HAK and base-game resource index exceeded the 60-second capture deadline.");
        }

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
                if (palette.PresentationState.SelectedRow is null && palette.PresentationState.Rows.Count > 0)
                {
                    palette.PresentationState.SelectedRow = palette.PresentationState.Rows[0];
                }

                if (palette.PresentationState.Tiles.Count == 0)
                {
                    throw new InvalidOperationException("The real shared Palette projection has no entries to display.");
                }

                palette.PresentationState.EnsurePreview(palette.PresentationState.Tiles[0]);
                provider.GetRequiredService<EditorService>().TryOpenEditor(ResourceType.Area, AreaResRef);
                await CaptureWhenReadyAsync(window, desktop, palette, output, runRoot, started);
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
        string sceneOutput,
        string runRoot,
        System.Diagnostics.Stopwatch started)
    {
        while (true)
        {
            EnsureWithinDeadline(started, "loading the real area editor and shared Palette");
            await Dispatcher.UIThread.InvokeAsync(() => { });
            var areaView = window.GetVisualDescendants().OfType<AreaEditorView>().FirstOrDefault();
            var paletteView = window.GetVisualDescendants().OfType<PaletteView>().FirstOrDefault();
            if (areaView?.DataContext is not AreaEditorViewModel viewModel
                || viewModel.IsBuildingScene
                || viewModel.AreaScene is null
                || paletteView is null
                || !paletteView.IsEffectivelyVisible
                || palette.PresentationState.Tiles.Count == 0
                || !palette.PresentationState.Tiles[0].HasPreview)
            {
                await Task.Delay(100);
                continue;
            }

            var scene = viewModel.AreaScene
                ?? throw new InvalidOperationException("The Area Editor lost its native scene during capture.");
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
        var requiredAssemblies = new[] { "Nwn.Preview", "Nwn.Toolset.Avalonia" };
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

    private static void EnsureWithinDeadline(System.Diagnostics.Stopwatch started, string operation) =>
        _ = Remaining(started).Ticks > 0
            ? true
            : throw new TimeoutException($"Timed out while {operation}.");
}
