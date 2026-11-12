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
    /// <see cref="IGameplayGeneration"/> in it), validates it off the engine thread and activates it on the engine thread
    /// while NWNX's bootstrap call is still running; the engine then runs the boot scripts as it does for the native
    /// entry (<c>ServerManager.Bootstrap</c>). Reload is off: the host loads one generation per process.
    /// </summary>
    public sealed class GameGeneration : IGameplayGeneration
    {
        private static readonly Lazy<int> SchemaVersion = new(() => NewestServerMigration(typeof(GameGeneration).Assembly.GetTypes()));

        private readonly IGenerationBoot _boot;
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
        {
            _boot = boot ?? throw new ArgumentNullException(nameof(boot));
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

            _sinks = NativeSinkFactory.Create();
            _sinkBinding = sinkBinding;
            sinkBinding.Bind(_sinks);

            _bootStarted = true;
            _boot.Activate(new HostedBoot(closures, redisConnection, hookControl, _sourceDirectory));

            Scripts = new HostedScriptDispatcher();
            MainLoop = new HostedMainLoop(hostCallbacks);
            _signals = new HostedSignalHandler();
            hostCallbacks.BindSignals(_signals);
            _hostCallbacks = hostCallbacks;

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Nothing yet: reload is off, so a generation is only retired when the process ends. P4-13 and P4-15 save online
        /// players' state, close windows and conversations and stop the timers here.
        /// </summary>
        public ValueTask BeginQuiescenceAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>Nothing yet: the server owns no work that outlives a callback while reload is off.</summary>
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
