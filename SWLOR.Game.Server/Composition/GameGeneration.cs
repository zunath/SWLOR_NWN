using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The game build as a generation of the permanent host (<c>SWLOR.Game.Host</c>, MaintainabilityRefactorHotReload.md
    /// section 3, "A generation"). The host loads this assembly, finds this class (the only
    /// <see cref="IGameplayGeneration"/> in it), validates it off the engine thread and activates it on the engine thread.
    /// Generation 1 activates while NWNX's bootstrap call is still running; the engine then runs the boot scripts as it
    /// does for the native entry (<c>ServerManager.Bootstrap</c>). A generation that activates after the module has loaded
    /// (a hot reload on a development or test server) rebuilds the service container and the script dispatch only: the
    /// engine runs no boot script for it, and rerunning the boot handlers and adopting the live world come later (P4-14).
    /// </summary>
    public sealed class GameGeneration : IGameplayGeneration
    {
        private static readonly Lazy<int> SchemaVersion = new(() => NewestServerMigration(typeof(GameGeneration).Assembly.GetTypes()));

        private readonly IGenerationBoot _boot;
        private readonly Action<string> _log;
        private string _sourceDirectory;
        private IHostCallbacks _hostCallbacks;
        private HostedSignalHandler _signals;
        private INativeSinkBinding _sinkBinding;
        private NativeSinkSet _sinks;
        private bool _bootStarted;

        /// <summary>The generation the host loads: the server's own boot.</summary>
        public GameGeneration()
            : this(new HostedServerBoot())
        {
        }

        public GameGeneration(IGenerationBoot boot)
            : this(boot, Console.WriteLine)
        {
        }

        /// <summary>For tests: <paramref name="log"/> stands in for the server console.</summary>
        public GameGeneration(IGenerationBoot boot, Action<string> log)
        {
            _boot = boot ?? throw new ArgumentNullException(nameof(boot));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>This build's identity: the game assembly's module version id.</summary>
        public string BuildId => typeof(GameGeneration).Assembly.ManifestModule.ModuleVersionId.ToString("N");

        /// <summary>The run-script dispatcher, once activated: the server's script executor.</summary>
        public IScriptDispatcher Scripts { get; private set; }

        /// <summary>The main-loop sink, once activated: the server's main loop for the frame the host delivers.</summary>
        public IMainLoopSink MainLoop { get; private set; }

        /// <summary>
        /// The newest server migration this build carries (<see cref="IServerMigration.Version"/>), or
        /// <see cref="MigrationService.RetiredServerMigrationVersion"/> when it carries none: a reload gate (P4-14) compares
        /// it with the database's <c>MigrationVersion</c>. The host does not gate the first activation.
        /// </summary>
        public int RequiredSchemaVersion => SchemaVersion.Value;

        /// <summary>The directory the host loaded this build from; the engine-test assembly is looked for there.</summary>
        public void ConfigureSourceDirectory(string directoryPath)
        {
            _sourceDirectory = directoryPath;
        }

        /// <summary>
        /// Builds the service container with validation and throws it away, off the engine thread, so a build whose
        /// registrations or constructors fail is rejected before the engine is touched.
        /// </summary>
        public ValueTask ValidateAsync(CancellationToken cancellationToken)
        {
            return new ValueTask(Task.Run(_boot.Validate, cancellationToken));
        }

        /// <summary>
        /// On the engine thread: binds this build's sinks to the host's native hooks, then boots the server with what the
        /// host hands it (its closure handler, its Redis connection, its hook control, the build's directory), which
        /// registers the loggers, sets the script execution provider, builds the container and registers the script
        /// handlers, then exposes the script dispatcher and the main loop and binds the signal handler. The hooks stay
        /// uninstalled until the game's boot handlers enable them, at the same moments as on the native entry.
        /// When the host says the module has already loaded (<see cref="IHostLifecycle.ModuleLoaded"/>, a reload), the
        /// server also takes the module-load signal the engine will not send again, and the activation is logged with how
        /// long it took: it is the new build's part of the reload pause.
        /// </summary>
        public ValueTask ActivateAsync(GenerationActivation activation, CancellationToken cancellationToken)
        {
            if (activation == null)
                throw new ArgumentNullException(nameof(activation));

            var services = activation.HostServices ?? throw new InvalidOperationException("The host hands the game no services.");
            var closures = Require<global::NWN.Core.ICoreFunctionHandler>(services);
            var redisConnection = Require<IRedisConnectionSource>(services);
            var hostCallbacks = Require<IHostCallbacks>(services);
            var hookControl = Require<IHookControl>(services);
            var sinkBinding = Require<INativeSinkBinding>(services);
            // A host that hands no lifecycle never reloads: every activation is a boot.
            var reload = services.Resolve(typeof(IHostLifecycle)) is IHostLifecycle { ModuleLoaded: true };
            var activationTime = reload ? Stopwatch.StartNew() : null;

            _sinks = NativeSinkFactory.Create();
            _sinkBinding = sinkBinding;
            sinkBinding.Bind(_sinks);

            _bootStarted = true;
            _boot.Activate(new HostedBoot(closures, redisConnection, hookControl, _sourceDirectory));
            if (reload)
                _boot.ResumeAfterModuleLoad();

            Scripts = new HostedScriptDispatcher();
            MainLoop = new HostedMainLoop(hostCallbacks);
            _signals = new HostedSignalHandler();
            hostCallbacks.BindSignals(_signals);
            _hostCallbacks = hostCallbacks;

            if (activationTime != null)
            {
                _log(FormattableString.Invariant(
                    $"Generation {activation.Generation} activated by a reload in {activationTime.Elapsed.TotalMilliseconds:F0} ms: ") +
                    "rebuilt the service container and the script dispatch only; no boot handler reran and nothing was adopted.");
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Nothing yet: the old build's dispose stops its timers and queues (<see cref="IServerTeardown"/>), and the host
        /// drops its pending closures. P4-15 saves online players' state and closes windows and conversations here.
        /// </summary>
        public ValueTask BeginQuiescenceAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>Nothing yet: the server owns no work that outlives a callback (the engine thread runs every callback).</summary>
        public ValueTask DrainAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// On the engine thread: unbinds this build's sinks, so every native hook runs the engine function again and none
        /// references a type of this build, stops taking signals, scripts and frames, then lets go of the server
        /// (<see cref="IServerTeardown"/>) and gives NWN.Core back to the host.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            if (_sinkBinding != null && _sinks != null)
                _sinkBinding.Unbind(_sinks);
            _sinkBinding = null;
            _sinks = null;

            if (_hostCallbacks != null && _signals != null)
                _hostCallbacks.UnbindSignals(_signals);

            Scripts = null;
            MainLoop = null;
            _signals = null;
            _hostCallbacks = null;

            // Also after an activation that failed part way: whatever the boot set up is let go of, and NWN.Core is the host's again.
            if (_bootStarted)
            {
                _bootStarted = false;
                _boot.Retire();
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>The newest server migration among <paramref name="types"/>, or the retired version when there is none.</summary>
        public static int NewestServerMigration(Type[] types)
        {
            var versions = types
                .Where(type => typeof(IServerMigration).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .Select(type => ((IServerMigration)Activator.CreateInstance(type)).Version)
                .ToList();

            return versions.Count == 0
                ? MigrationService.RetiredServerMigrationVersion
                : Math.Max(versions.Max(), MigrationService.RetiredServerMigrationVersion);
        }

        private static TService Require<TService>(IHostServices services)
            where TService : class
        {
            return services.Resolve(typeof(TService)) as TService
                   ?? throw new InvalidOperationException($"The host hands the game no {typeof(TService).Name}.");
        }
    }
}
