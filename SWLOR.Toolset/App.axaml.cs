using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset
{
    public class App : Application
    {
        private ServiceProvider? _serviceProvider;

        /// <summary>
        /// The composed container, for the few views that need a shared service the view model does
        /// not carry - see <see cref="Viewport.ViewportDisplayOptions"/>. Null before startup finishes.
        /// </summary>
        public IServiceProvider? Services => _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                desktop.MainWindow = new MainWindow(
                    _serviceProvider.GetRequiredService<ShellViewModel>(),
                    _serviceProvider.GetRequiredService<ToolsetSettings>());
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Registers every service, lookup, and view model the shell needs. Game-data services
        /// (2DA/TLK/resource index/lookups/game-code index) are all rooted at the repository
        /// found by walking up from the executable's directory; if that repository layout can't be
        /// found (e.g. the exe was copied out on its own), those registrations are simply skipped -
        /// consumers declare them as optional constructor parameters and get null instead of a
        /// missing-service exception.
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            var settings = ToolsetSettings.Load();
            services.AddSingleton(settings);

            services.AddSingleton<OutputLogService>();
            services.AddSingleton<Services.IEditorPromptService, Services.EditorPromptService>();
            // The index is what lets a workspace hand back a base-game blueprint the module has no file
            // of its own for - the palette's Standard group depends on it.
            services.AddSingleton<Func<string, ModuleWorkspace>>(sp =>
                path => new ModuleWorkspace(path, sp.GetService<ResourceIndex>()));
            services.AddSingleton<WorkspaceContext>();
            services.AddSingleton<ModuleFileWatcher>();

            var repoRoot = AutoDetectRepoRoot();
            RegisterGameDataServices(services, repoRoot, settings);

            services.AddSingleton(sp => new Editors.LookupOptionProvider(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetService<AppearanceService>(),
                sp.GetService<PortraitService>(),
                sp.GetService<PlaceableAppearanceService>(),
                sp.GetService<DoorTypeService>(),
                sp.GetService<SoundService>(),
                sp.GetService<TwoDaLookupService>()));
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
                sp.GetRequiredService<Workspace.ScriptLanguageService>()));

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
                sp.GetRequiredService<ToolsetSettings>()));
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
                sp.GetRequiredService<ToolsetSettings>()));
            services.AddSingleton<SearchViewModel>();
            services.AddSingleton<OutputViewModel>();
            services.AddSingleton(sp => new ValidationViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<Func<Editors.EditorService>>(),
                sp.GetService<IGameCodeIndex>(),
                sp.GetService<ResourceIndex>()));
            // Constructed explicitly so the settings the layout's divider positions live in are wired in
            // rather than left to constructor selection.
            services.AddSingleton(sp => new ToolsetDockFactory(
                sp.GetRequiredService<ModuleExplorerViewModel>(),
                sp.GetRequiredService<PropertiesViewModel>(),
                sp.GetRequiredService<SearchViewModel>(),
                sp.GetRequiredService<OutputViewModel>(),
                sp.GetRequiredService<ValidationViewModel>(),
                sp.GetRequiredService<PaletteViewModel>(),
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
            var hakBuilderConfigPath = Path.Combine(repoRoot, "Build", "hakbuilder.json");
            var swlorHaksRoot = Path.Combine(repoRoot, "SWLOR_Haks");

            var hasTwoDa = Directory.Exists(sw2DaDirectory);
            var hasTlk = File.Exists(swTlkJsonPath);

            if (hasTwoDa)
                services.AddSingleton(new TwoDaService(sw2DaDirectory));

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
            ReportNwnInstall(services, nwnInstallPath, settings.NwnInstallOverride);

            if (hasTlk)
            {
                // The base game's dialog.tlk as well as SWLOR's own, because the base-game palettes name
                // their categories by strref into it - without this the Standard palette's folders read
                // as "Category 6782" instead of "Containers & Switches". It lives under lang/<code>/data,
                // not the data folder the resource index uses.
                services.AddSingleton(TlkService.Load(swTlkJsonPath, FindBaseTlk(nwnInstallPath)));
            }

            if (File.Exists(hakBuilderConfigPath) && Directory.Exists(swlorHaksRoot))
            {
                // Attach the base-game KEY/BIF layer when an NWN:EE install is present so base
                // resources (models, base blueprints) resolve; hak-only otherwise.
                KeyBifCatalog? baseLayer = null;
                try
                {
                    if (nwnInstallPath != null)
                        baseLayer = KeyBifCatalog.Load(Path.Combine(nwnInstallPath, "data"));
                }
                catch (Exception)
                {
                    // A broken install must not stop the toolset; hak layers still work.
                }

                services.AddSingleton(ResourceIndex.FromHakBuilderConfig(hakBuilderConfigPath, swlorHaksRoot, baseLayer));

                // The area 3D view needs both, and both need the ResourceIndex above -
                // registered inside this same guard so resolving either never hits a missing
                // ResourceIndex dependency when the repo layout wasn't found.
                services.AddSingleton(sp => new Domain.GameData.Lookups.TilesetCatalog(sp.GetRequiredService<ResourceIndex>()));
                services.AddSingleton(sp => new Domain.Render.TileModelCache(sp.GetRequiredService<ResourceIndex>()));

                // Walkmesh cache: resolves each tile model's .wok and classifies faces via
                // surfacemat.2da's Walk column (for the overlay color + placement height-snap).
                services.AddSingleton(sp => new Domain.Render.TileWalkmeshCache(
                    sp.GetRequiredService<ResourceIndex>(),
                    BuildSurfaceWalkability(sp.GetService<TwoDaService>())));
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
        /// Walks up from the executable's directory looking for the repo root (matching the
        /// pattern used by ResourceIndexTests/LookupServiceTests): both "Build/hakbuilder.json" and
        /// "SWLOR_Haks" must be present. Returns null (never throws) if not found.
        /// </summary>
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

        private static string? AutoDetectRepoRoot()
        {
            try
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
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
