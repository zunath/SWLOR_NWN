using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Placeables;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset
{
    public class App : Application
    {
        private const double GalleryLoadAheadPixels = 600;
        private ServiceProvider? _serviceProvider;

        /// <summary>
        /// The composed container, for the few views that need a shared service the view model does
        /// not carry - see <see cref="Viewport.ViewportDisplayOptions"/>. Null before startup finishes.
        /// </summary>
        public IServiceProvider? Services => _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // Has to be in place before the shell's docks are templated, and it is a class handler
            // rather than anything the layout owns, so it belongs with the styles rather than with
            // the container built in OnFrameworkInitializationCompleted.
            Shell.Controls.RailToolTabs.Register();
            Shell.Controls.ReadableComboBoxDropDowns.Register();
        }

        /// <summary>
        /// Keeps inline behavior galleries flowing as the builder scrolls without constructing every
        /// creature, placeable, or VFX preview when the form first opens.
        /// </summary>
        private void OnBehaviorGalleryScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer ||
                scrollViewer.DataContext is not BehaviorFieldViewModel field ||
                !field.CanLoadMore)
            {
                return;
            }

            var remaining =
                scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
            if (remaining <= GalleryLoadAheadPixels)
                field.LoadMoreGalleryCommand.Execute(null);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Settings are the only disk-backed dependency needed to size and place the first
                // window. Everything else includes game archives, TLKs, source indexes, and ~130 hak
                // folders, so composing it before assigning MainWindow made the process look hung.
                var settings = ToolsetSettings.Load();
                var window = new MainWindow(settings);
                desktop.MainWindow = window;

                window.Opened += async (_, _) => await BootstrapAsync(window, settings);
                desktop.Exit += (_, _) =>
                {
                    _serviceProvider?.Dispose();
                    _serviceProvider = null;
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Composes only the lightweight object graph after the first window has painted, attaches the
        /// usable shell, and then lets game-data indexes continue warming in the background.
        /// </summary>
        private async Task BootstrapAsync(MainWindow window, ToolsetSettings settings)
        {
            var stopwatch = Stopwatch.StartNew();
            ServiceProvider? result = null;

            try
            {
                result = await Task.Run(() =>
                {
                    var services = new ServiceCollection();
                    ConfigureServices(services, settings);
                    return services.BuildServiceProvider();
                }).ConfigureAwait(true);

                // The user may close the lightweight window during the short composition step.
                if (!window.IsVisible)
                {
                    result.Dispose();
                    return;
                }

                _serviceProvider = result;
                var shell = result.GetRequiredService<ShellViewModel>();
                window.AttachViewModel(shell);

                stopwatch.Stop();
                result.GetRequiredService<OutputLogService>().AppendLine(
                    $"Interactive shell ready in {stopwatch.ElapsedMilliseconds}ms; game data is loading in the background.");

                var gameDataReady = WarmGameDataServicesAsync(result);
                await shell.InitializeAsync().ConfigureAwait(true);

                // ThumbnailService resets its queues when the workspace opens. Wait for both the
                // workspace and immutable game data before warming the expensive segmented race
                // previews, otherwise startup can discard the work and leave those first seven
                // gallery cells spinning until the builder opens the Appearance tab.
                _ = WarmDynamicAppearancePreviewsAsync(result, gameDataReady);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                if (result != null)
                {
                    if (ReferenceEquals(_serviceProvider, result))
                        _serviceProvider = null;
                    result.Dispose();
                }

                window.ShowStartupError(
                    $"Toolset startup failed after {stopwatch.ElapsedMilliseconds}ms: {ex.GetBaseException().Message}");
            }
        }

        /// <summary>
        /// Warms immutable game-data services without holding the shell hostage. Features that consume
        /// one of these services can wait for that service while the module explorer and local editors
        /// remain usable.
        /// </summary>
        private static async Task WarmGameDataServicesAsync(IServiceProvider provider)
        {
            var stopwatch = Stopwatch.StartNew();
            var log = provider.GetRequiredService<OutputLogService>();

            try
            {
                var resourceTask = Task.Run(
                    () => provider.GetService<ResourceIndex>()?.EnsureInitialized());
                var tlkTask = Task.Run(
                    () => provider.GetService<TlkService>()?.GetCustomText(0));
                var editorTask = Task.Run(() =>
                {
                    _ = provider.GetService<IGameCodeIndex>();
                    // EditorService is deliberately absent from the shell's constructor graph. Build
                    // it beside the source index so an early document click does not inherit that cost.
                    _ = provider.GetService<Editors.EditorService>();
                });

                await Task.WhenAll(resourceTask, tlkTask, editorTask).ConfigureAwait(false);

                await Task.Run(() =>
                {
                    _ = provider.GetService<AppearanceService>();
                    _ = provider.GetService<PortraitService>();
                    _ = provider.GetService<PlaceableAppearanceService>();
                    _ = provider.GetService<PlaceableModelCatalog>();
                    _ = provider.GetService<DoorTypeService>();
                    _ = provider.GetService<SoundService>();
                    _ = provider.GetService<TwoDaLookupService>();
                    _ = provider.GetService<WaypointAppearanceService>();
                    _ = provider.GetService<BaseItemIconService>();
                    _ = provider.GetService<Domain.GameData.Lookups.TilesetCatalog>();
                    _ = provider.GetService<Domain.Render.TileModelCache>();
                    _ = provider.GetService<Domain.Render.TileWalkmeshCache>();
                }).ConfigureAwait(false);

                stopwatch.Stop();
                log.AppendLine($"Background game data ready in {stopwatch.ElapsedMilliseconds}ms.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                log.AppendLine(
                    $"Background game-data load failed after {stopwatch.ElapsedMilliseconds}ms: " +
                    ex.GetBaseException().Message);
            }
        }

        /// <summary>
        /// Builds the seven stock dynamic-race thumbnails after workspace initialization has made
        /// their cache identity stable. These are appearance.2da rows 0 through 6: Dwarf, Elf,
        /// Gnome, Halfling, Half-Elf, Half-Orc, and Human.
        /// </summary>
        private static async Task WarmDynamicAppearancePreviewsAsync(
            IServiceProvider provider,
            Task gameDataReady)
        {
            try
            {
                await gameDataReady.ConfigureAwait(false);
                provider.GetService<ThumbnailService>()?.WarmGenericSegmentedCreaturePreviews();
            }
            catch (Exception ex)
            {
                provider.GetRequiredService<OutputLogService>().AppendLine(
                    "Dynamic creature preview warm-up failed: " + ex.GetBaseException().Message);
            }
        }

        /// <summary>
        /// Registers every service, lookup, and view model the shell needs. Game-data services
        /// (2DA/TLK/resource index/lookups/game-code index) are all rooted at the repository
        /// containing the configured module. The executable's repository remains a fallback for a
        /// missing or invalid module setting. If neither layout can be found, those registrations
        /// are simply skipped - consumers declare them as optional constructor parameters and get
        /// null instead of a missing-service exception.
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, ToolsetSettings settings)
        {
            services.AddSingleton(settings);

            services.AddSingleton<OutputLogService>();
            services.AddSingleton<Services.IEditorPromptService, Services.EditorPromptService>();
            services.AddSingleton<Services.IExternalLinkService, Services.ExternalLinkService>();
            // The index is what lets a workspace hand back a base-game blueprint the module has no file
            // of its own for - the palette's Standard group depends on it.
            services.AddSingleton<Func<string, ModuleWorkspace>>(sp =>
                path => new ModuleWorkspace(path, sp.GetService<ResourceIndex>()));
            services.AddSingleton<WorkspaceContext>();
            services.AddSingleton<ModuleFileWatcher>();
            services.AddSingleton<Workspace.PlaceableIndexService>();

            var repoRoot = ResolveRepoRoot(settings);
            RegisterGameDataServices(services, repoRoot, settings);
            services.AddSingleton<Workspace.ModuleCustomContentService>();

            services.AddSingleton(sp => new Editors.LookupOptionProvider(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetService<AppearanceService>(),
                sp.GetService<PortraitService>(),
                sp.GetService<PlaceableAppearanceService>(),
                sp.GetService<DoorTypeService>(),
                sp.GetService<SoundService>(),
                sp.GetService<TwoDaLookupService>(),
                sp.GetService<WaypointAppearanceService>()));
            services.AddSingleton<Editors.Placeables.VfxPreviewService>();
            // One answer to "is a module-wide operation running", shared by every panel and editor
            // tab that writes to the module. Registered before its consumers so all of them take
            // the same instance; the shell is its only writer.
            services.AddSingleton<Services.ModuleMutationLock>();
            services.AddSingleton(sp => new Editors.EditorService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<Editors.LookupOptionProvider>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<ToolsetDockFactory>(),
                sp.GetRequiredService<Services.IEditorPromptService>(),
                sp.GetService<IGameCodeIndex>(),
                sp.GetService<Domain.GameData.Lookups.TilesetCatalog>(),
                sp.GetService<Domain.Render.TileModelCache>(),
                sp.GetService<ResourceIndex>(),
                sp.GetService<PlaceableAppearanceService>(),
                sp.GetService<DoorTypeService>(),
                sp.GetService<Domain.Render.TileWalkmeshCache>(),
                sp.GetService<TlkService>(),
                sp.GetService<WaypointAppearanceService>(),
                sp.GetRequiredService<Workspace.BlueprintPreviewRenderer>(),
                sp.GetRequiredService<Workspace.ScriptLanguageService>(),
                sp.GetRequiredService<ProblemsViewModel>(),
                sp.GetRequiredService<Services.ScriptCompileService>(),
                sp.GetService<PlaceableModelCatalog>(),
                sp.GetService<ThumbnailService>(),
                sp.GetRequiredService<Workspace.PlaceableIndexService>(),
                sp.GetService<TwoDaService>(),
                sp.GetRequiredService<Editors.Placeables.VfxPreviewService>(),
                sp.GetService<PortraitService>(),
                sp.GetService<AppearanceService>(),
                sp.GetRequiredService<Services.ModuleMutationLock>(),
                sp.GetService<CategoryService>(),
                moduleCustomContent: sp.GetRequiredService<Workspace.ModuleCustomContentService>(),
                externalLinks: sp.GetRequiredService<Services.IExternalLinkService>(),
                tlkEditorSource: sp.GetService<Editors.Tlk.TlkEditorSource>()));

            // One parsed engine header shared by every script tab, built lazily on first use: the
            // header is 13,870 lines, so parsing it per tab would be wasteful and parsing it at
            // startup would delay a window that may never open a script.
            services.AddSingleton(sp => new Workspace.ScriptLanguageService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>()));

            // The explorer needs to open editors, but EditorService depends on the dock factory,
            // which depends on the explorer — a Func breaks the construction cycle.
            services.AddSingleton<Func<Editors.EditorService>>(sp =>
                () => sp.GetRequiredService<Editors.EditorService>());

            services.AddSingleton<Services.SaveService>();
            services.AddSingleton<Services.PackService>();
            services.AddSingleton<Services.ErfArchiveService>();
            services.AddSingleton<PropertiesViewModel>();
            services.AddSingleton(sp => new ModuleExplorerViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<PropertiesViewModel>(),
                sp.GetRequiredService<CategoryService>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<Func<Editors.EditorService>>(),
                // Optional: only registered when the repo layout resolved, and the new-area wizard
                // degrades to "no tilesets available" without it.
                sp.GetService<Domain.GameData.Lookups.TilesetCatalog>(),
                sp.GetRequiredService<Services.IEditorPromptService>(),
                sp.GetRequiredService<ToolsetSettings>(),
                sp.GetRequiredService<Services.ModuleMutationLock>()));
            services.AddSingleton(sp => new CategoryService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                // Optional: the base-game palettes name their categories by TLK reference, so without
                // the TLK they import as renameable placeholders rather than real names.
                sp.GetService<TlkService>(),
                // Optional: the source of the Standard palette. Without it the palette shows the module's
                // own content only, which is what it did before the split existed.
                sp.GetService<ResourceIndex>()));
            // Every game-data dependency here is optional: without a resolved repo layout the renderer
            // reports itself unavailable and the palette falls back to letter glyphs rather than failing.
            services.AddSingleton(sp => new BlueprintPreviewRenderer(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetService<ResourceIndex>(),
                sp.GetService<AppearanceService>(),
                sp.GetService<PlaceableAppearanceService>(),
                sp.GetService<DoorTypeService>(),
                sp.GetService<WaypointAppearanceService>(),
                sp.GetService<BaseItemIconService>(),
                sp.GetService<PortraitService>(),
                sp.GetService<TwoDaService>(),
                sp.GetService<TlkService>()));
            services.AddSingleton(sp => new ThumbnailService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<BlueprintPreviewRenderer>()));
            services.AddSingleton(sp => new PaletteViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<CategoryService>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<Func<Editors.EditorService>>(),
                // The palette places into whichever area document is in front. Resolved lazily for the
                // same construction-cycle reason as EditorService above.
                () => sp.GetRequiredService<ToolsetDockFactory>().ActivePlacementTarget,
                sp.GetRequiredService<ThumbnailService>(),
                sp.GetRequiredService<Services.IEditorPromptService>(),
                sp.GetService<Domain.GameData.Lookups.TilesetCatalog>(),
                sp.GetService<TlkService>(),
                sp.GetRequiredService<ToolsetSettings>(),
                // The palette writes straight to the module, so its create/delete controls follow the
                // same lock that packing and validation raise. Taken as the shared object rather than
                // resolved back through the shell: this panel is constructed as part of building the
                // shell, and asking the container for a ShellViewModel from here is reentrant.
                sp.GetRequiredService<Services.ModuleMutationLock>()));
            services.AddSingleton(sp => new AreaContentsViewModel(
                sp.GetRequiredService<Services.IEditorPromptService>()));
            services.AddSingleton<SearchViewModel>();
            services.AddSingleton<OutputViewModel>();
            services.AddSingleton(sp => new ValidationViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<Func<Editors.EditorService>>(),
                () => sp.GetService<IGameCodeIndex>(),
                () => sp.GetService<ResourceIndex>(),
                sp.GetRequiredService<Services.ModuleMutationLock>()));
            // Constructed explicitly so the settings the layout's divider positions live in are wired in
            // rather than left to constructor selection.
            services.AddSingleton(sp => new ToolsetDockFactory(
                sp.GetRequiredService<ModuleExplorerViewModel>(),
                sp.GetRequiredService<PropertiesViewModel>(),
                sp.GetRequiredService<SearchViewModel>(),
                sp.GetRequiredService<OutputViewModel>(),
                sp.GetRequiredService<ValidationViewModel>(),
                sp.GetRequiredService<PaletteViewModel>(),
                sp.GetRequiredService<ProblemsViewModel>(),
                sp.GetRequiredService<ScriptReferenceViewModel>(),
                sp.GetRequiredService<AreaContentsViewModel>(),
                sp.GetRequiredService<ToolsetSettings>()));
            services.AddSingleton<ProblemsViewModel>();
            services.AddSingleton(sp => new ScriptReferenceViewModel(
                sp.GetRequiredService<Workspace.ScriptLanguageService>(),
                sp.GetRequiredService<Services.IExternalLinkService>()));
            services.AddSingleton(sp => new Services.ScriptCompileService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<ToolsetSettings>()));
            services.AddSingleton(sp => new Viewport.ViewportDisplayOptions(
                sp.GetRequiredService<ToolsetSettings>()));
            services.AddSingleton<ShellViewModel>();
        }

        private static void RegisterGameDataServices(
            IServiceCollection services, string? repoRoot, ToolsetSettings settings)
        {
            var gameServerSourceRoot = repoRoot == null ? null : Path.Combine(repoRoot, "SWLOR.Game.Server");
            services.AddSingleton<IGameCodeIndex>(_ => new GameCodeIndex(gameServerSourceRoot));

            if (repoRoot == null)
                return;

            var sw2DaDirectory = Path.Combine(repoRoot, "SWLOR_Haks", "sw_2da");
            var swTlkJsonPath = Path.Combine(repoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
            var swTlkBinaryPath = Path.Combine(repoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk");
            var hakBuilderConfigPath = Path.Combine(repoRoot, "Build", "hakbuilder.json");
            var swlorHaksRoot = Path.Combine(repoRoot, "SWLOR_Haks");

            var hasTwoDa = Directory.Exists(sw2DaDirectory);
            var hasTlk = File.Exists(swTlkJsonPath);

            // Registered even when one path is missing so the Tools menu can explain why the
            // repository-only editor is disabled instead of silently disappearing.
            services.AddSingleton(new Editors.Tlk.TlkEditorSource(
                swTlkJsonPath,
                swTlkBinaryPath,
                sw2DaDirectory));

            // Located once and reused: both the resource index and the base TLK below need it.
            string? nwnInstallPath = null;
            try
            {
                nwnInstallPath = NwnInstallLocator.Locate(settings.NwnInstallOverride);
            }
            catch (Exception)
            {
                // A broken or absent install must not stop the toolset; hak layers still work.
            }

            // Reported rather than left silent. Without the base game the Standard palette is empty and
            // base-game models and category names are missing - all of which look like bugs in this
            // toolset unless it says so, which is exactly how it read before this line existed.
            if (hasTlk)
            {
                // The base game's dialog.tlk as well as SWLOR's own, because the base-game palettes name
                // their categories by strref into it - without this the Standard palette's folders read
                // as "Category 6782" instead of "Containers & Switches". It lives under lang/<code>/data,
                // not the data folder the resource index uses.
                services.AddSingleton(sp => TlkService.LoadDeferredWithOptionalBase(
                    swTlkJsonPath,
                    FindBaseTlk(nwnInstallPath),
                    warning => sp.GetRequiredService<OutputLogService>().AppendLine(warning)));
            }

            ReportNwnInstall(services, nwnInstallPath, settings.NwnInstallOverride);

            if (File.Exists(hakBuilderConfigPath) && Directory.Exists(swlorHaksRoot))
            {
                // KEY parsing and HAK indexing both belong to ResourceIndex's background initialization
                // task. Prefer the module's packed, authoritative HAK stack: indexing its archive tables
                // is dramatically cheaper than walking ~160,000 loose hakbuilder source files only to
                // replace that fallback stack as soon as WorkspaceOpened reads the same module.ifo.
                Func<KeyBifCatalog?>? loadBaseLayer = nwnInstallPath == null
                    ? null
                    : () => KeyBifCatalog.Load(Path.Combine(nwnInstallPath, "data"));
                var moduleHakLayers = ResolveStartupHakLayers(settings.ModuleRoot, NwnIniProfile.Load());
                services.AddSingleton(moduleHakLayers == null
                    ? ResourceIndex.FromHakBuilderConfigDeferred(
                        hakBuilderConfigPath,
                        swlorHaksRoot,
                        loadBaseLayer)
                    : ResourceIndex.CreateDeferred(moduleHakLayers, loadBaseLayer));
                services.AddSingleton(sp => new TwoDaService(sp.GetRequiredService<ResourceIndex>()));

                // The area 3D view needs both, and both need the ResourceIndex above -
                // registered inside this same guard so resolving either never hits a missing
                // ResourceIndex dependency when the repo layout wasn't found.
                services.AddSingleton(sp => new Domain.GameData.Lookups.TilesetCatalog(sp.GetRequiredService<ResourceIndex>()));
                services.AddSingleton(sp => new Domain.Render.TileModelCache(sp.GetRequiredService<ResourceIndex>()));

                // Walkmesh cache: resolves each tile model's .wok and classifies faces via
                // surfacemat.2da's Walk column (for the overlay color + placement height-snap).
                services.AddSingleton(sp => new Domain.Render.TileWalkmeshCache(
                    sp.GetRequiredService<ResourceIndex>(),
                    () => BuildSurfaceWalkability(sp.GetService<TwoDaService>())));
            }
            else if (hasTwoDa)
            {
                services.AddSingleton(new TwoDaService(sw2DaDirectory));
            }

            // AppearanceService/PortraitService are only registered when their 2DA/TLK
            // dependencies actually resolved above - PropertiesViewModel takes both as optional
            // constructor parameters, so a missing registration degrades to null (raw
            // appearance/portrait ids shown instead of resolved names) rather than a DI failure.
            if (hasTwoDa && hasTlk)
            {
                services.AddSingleton(sp =>
                    new AppearanceService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                services.AddSingleton(sp =>
                    new PlaceableAppearanceService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                // The placeable editor's model grid, which keeps the rows the dropdown service drops
                // for having no label - two thirds of the table.
                services.AddSingleton(sp =>
                    new PlaceableModelCatalog(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                services.AddSingleton(sp =>
                    new DoorTypeService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                services.AddSingleton(sp =>
                    new WaypointAppearanceService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                services.AddSingleton(sp =>
                    new SoundService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
                services.AddSingleton(sp =>
                    new TwoDaLookupService(sp.GetRequiredService<TwoDaService>(), sp.GetRequiredService<TlkService>()));
            }

            if (hasTwoDa)
            {
                services.AddSingleton(sp =>
                    new PortraitService(sp.GetRequiredService<TwoDaService>()));
                services.AddSingleton(sp =>
                    new BaseItemIconService(sp.GetRequiredService<TwoDaService>()));
            }
        }

        /// <summary>
        /// The base game's dialog.tlk, or null when there is no install or no localized copy of it.
        /// </summary>
        /// <remarks>
        /// Not in the install's data folder with everything else - it sits under lang/&lt;code&gt;/data, one
        /// per language. English first because that is what this module authors in; the rest are tried so
        /// a non-English install still resolves names rather than silently showing strref numbers.
        /// </remarks>
        private static string? FindBaseTlk(string? installPath)
        {
            if (installPath == null)
                return null;

            var languages = new[] { "en", "fr", "de", "it", "es", "pl" };
            foreach (var language in languages)
            {
                var candidate = Path.Combine(installPath, "lang", language, "data", "dialog.tlk");
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Resolves the saved module's runtime HAK order before the workspace opens. Null means the
        /// runtime stack cannot be discovered, so startup should retain the loose source fallback;
        /// an empty list is meaningful and means the module authoritatively assigns no available HAKs.
        /// </summary>
        private static IReadOnlyList<ResourceIndex.HakLayer>? ResolveStartupHakLayers(
            string? moduleRoot,
            NwnIniProfile profile)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot) || profile.HakDirectory == null)
                return null;

            try
            {
                var ifoPath = Path.Combine(moduleRoot, "ifo", "module.ifo.json");
                if (!File.Exists(ifoPath))
                    return null;

                var ifo = IfoDocument.Load(ifoPath);
                var resolution = profile.ResolveHakLayers(ifo.HakNames);
                return resolution.MissingHakNames.Count == 0
                    ? resolution.Layers
                    : null;
            }
            catch (Exception)
            {
                // A missing/malformed module or profile falls back to the repository source layers.
                return null;
            }
        }

        /// <summary>
        /// Builds the walkmesh face-walkability predicate from surfacemat.2da's "Walk" column
        /// (1 = walkable). When the table can't be read, treats every surface as walkable so
        /// height-snapping still works everywhere (the overlay just shows all faces as walkable)
        /// rather than snapping to nothing.
        /// </summary>
        private static Func<int, bool> BuildSurfaceWalkability(TwoDaService? twoDa)
        {
            if (twoDa == null || !twoDa.TryGetTable("surfacemat", out var table) || table == null)
                return _ => true;

            var walkable = new bool[table.RowCount];
            for (var row = 0; row < table.RowCount; row++)
            {
                try
                {
                    walkable[row] = table.GetInt(row, "Walk") == 1;
                }
                catch (FormatException)
                {
                    walkable[row] = false; // A malformed Walk cell is treated as blocked, not fatal.
                }
            }

            return material => material >= 0 && material < walkable.Length && walkable[material];
        }

        /// <summary>
        /// Resolves game data beside the configured module first. This matters when a published
        /// toolset executable opens a module in another checkout: its 2DA, TLK, haks, and game-code
        /// index must all come from the module's repository rather than the executable's checkout.
        /// The executable directory remains the compatibility fallback.
        /// </summary>
        private static string? ResolveRepoRoot(ToolsetSettings settings) =>
            FindRepoRoot(settings.ModuleRoot) ?? FindRepoRoot(AppContext.BaseDirectory);

        /// <summary>
        /// Queues a log line naming the NWN:EE install that was found, or listing where it looked when
        /// there was none. Deferred onto the log service rather than written here, because logging is a
        /// service that does not exist yet while services are still being registered.
        /// </summary>
        private static void ReportNwnInstall(
            IServiceCollection services, string? resolvedPath, string? overridePath)
        {
            var message = resolvedPath != null
                ? $"NWN:EE install: {resolvedPath}"
                : "No NWN:EE install found - base-game blueprints, models and the Standard palette will be " +
                  "unavailable. Checked: " + string.Join("; ", NwnInstallLocator.ProbedPaths(overridePath)) +
                  ". Set an explicit path in settings.json (nwnInstallOverride) to override.";

            services.AddSingleton(new StartupNotice(message));
        }

        private static string? FindRepoRoot(string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return null;

            try
            {
                var fullStartPath = Path.GetFullPath(startPath);
                var current = new DirectoryInfo(
                    File.Exists(fullStartPath)
                        ? Path.GetDirectoryName(fullStartPath)!
                        : fullStartPath);
                while (current != null)
                {
                    var hakBuilderConfig = Path.Combine(current.FullName, "Build", "hakbuilder.json");
                    var haksDirectory = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (File.Exists(hakBuilderConfig) && Directory.Exists(haksDirectory))
                        return current.FullName;

                    current = current.Parent;
                }
            }
            catch (Exception)
            {
                // Any I/O failure while probing just means repo-root auto-detection found nothing.
            }

            return null;
        }
    }
}
