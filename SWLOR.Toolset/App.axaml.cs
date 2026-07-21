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

                desktop.MainWindow = new MainWindow(_serviceProvider.GetRequiredService<ShellViewModel>());
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
            services.AddSingleton<Func<string, ModuleWorkspace>>(_ => path => new ModuleWorkspace(path));
            services.AddSingleton<WorkspaceContext>();
            services.AddSingleton<ModuleFileWatcher>();

            var repoRoot = AutoDetectRepoRoot();
            RegisterGameDataServices(services, repoRoot, settings);

            services.AddSingleton(sp => new Editors.LookupOptionProvider(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetService<AppearanceService>(),
                sp.GetService<PortraitService>()));
            services.AddSingleton(sp => new Editors.EditorService(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<Editors.LookupOptionProvider>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetRequiredService<ToolsetDockFactory>(),
                sp.GetService<IGameCodeIndex>(),
                sp.GetRequiredService<ModelPreviewViewModel>()));

            // The explorer needs to open editors, but EditorService depends on the dock factory,
            // which depends on the explorer — a Func breaks the construction cycle.
            services.AddSingleton<Func<Editors.EditorService>>(sp =>
                () => sp.GetRequiredService<Editors.EditorService>());

            services.AddSingleton<Services.SaveService>();
            services.AddSingleton<Services.PackService>();
            services.AddSingleton(sp => new ModelPreviewViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetService<AppearanceService>(),
                sp.GetService<ResourceIndex>(),
                sp.GetService<TwoDaService>(),
                sp.GetService<TlkService>(),
                sp.GetService<PlaceableAppearanceService>(),
                sp.GetService<DoorTypeService>()));
            services.AddSingleton<PropertiesViewModel>();
            services.AddSingleton<ModuleExplorerViewModel>();
            services.AddSingleton<SearchViewModel>();
            services.AddSingleton<OutputViewModel>();
            services.AddSingleton(sp => new ValidationViewModel(
                sp.GetRequiredService<WorkspaceContext>(),
                sp.GetRequiredService<OutputLogService>(),
                sp.GetService<IGameCodeIndex>(),
                sp.GetService<ResourceIndex>()));
            services.AddSingleton<ToolsetDockFactory>();
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

            if (hasTlk)
                services.AddSingleton(TlkService.Load(swTlkJsonPath));

            if (File.Exists(hakBuilderConfigPath) && Directory.Exists(swlorHaksRoot))
            {
                // Attach the base-game KEY/BIF layer when an NWN:EE install is present so base
                // resources (models, base blueprints) resolve; hak-only otherwise.
                KeyBifCatalog? baseLayer = null;
                try
                {
                    var installPath = NwnInstallLocator.Locate(settings.NwnInstallOverride);
                    if (installPath != null)
                        baseLayer = KeyBifCatalog.Load(Path.Combine(installPath, "data"));
                }
                catch (Exception)
                {
                    // A broken install must not stop the toolset; hak layers still work.
                }

                services.AddSingleton(ResourceIndex.FromHakBuilderConfig(hakBuilderConfigPath, swlorHaksRoot, baseLayer));
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
            }

            if (hasTwoDa)
            {
                services.AddSingleton(sp =>
                    new PortraitService(sp.GetRequiredService<TwoDaService>()));
            }
        }

        /// <summary>
        /// Walks up from the executable's directory looking for the repo root (matching the
        /// pattern used by ResourceIndexTests/LookupServiceTests): both "Build/hakbuilder.json" and
        /// "SWLOR_Haks" must be present. Returns null (never throws) if not found.
        /// </summary>
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
