using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// The game build as the host's generation: validation off the engine thread, activation with what the host hands it,
/// then the script dispatcher, the main loop and the signal handler; dispose unbinds and lets go of the server.
/// </summary>
[TestFixture]
public class GameGenerationTests
{
    private sealed class RecordingBoot : IGenerationBoot
    {
        public List<string> Calls { get; } = new();
        public HostedBoot Boot { get; private set; }
        public int ValidatedOnThread { get; private set; }
        public Exception ActivationFailure { get; set; }

        public void Validate()
        {
            ValidatedOnThread = Environment.CurrentManagedThreadId;
            Calls.Add("validate");
        }

        public void Activate(HostedBoot boot)
        {
            Calls.Add("activate");
            Boot = boot;
            if (ActivationFailure != null)
                throw ActivationFailure;
        }

        public void ResumeAfterModuleLoad() => Calls.Add("resume");

        public List<string> Signals { get; } = new();

        public void ProcessSignal(string signal) => Signals.Add(signal);

        public IReadOnlyList<ReloadStepResult> Rebuilt { get; set; } = Array.Empty<ReloadStepResult>();
        public IReadOnlyList<ReloadDigestLine> Caches { get; set; } = new[] { new ReloadDigestLine("cache", "Cache.Owner", 3, "00000000000000aa") };
        public IReadOnlyList<ReloadDigestLine> Adopted { get; set; } = new[] { new ReloadDigestLine("world", "world.dockIds", 2, "00000000000000bb") };
        public Exception CarryFailure { get; set; }
        public List<Action> NextFrame { get; } = new();

        public IReadOnlyList<ReloadStepResult> RebuildAfterReload()
        {
            Calls.Add("rebuild");
            return Rebuilt;
        }

        public IReadOnlyList<ReloadStepResult> CarryForReload()
        {
            Calls.Add("carry");
            if (CarryFailure != null)
                throw CarryFailure;
            return Array.Empty<ReloadStepResult>();
        }

        public IReadOnlyList<ReloadDigestLine> DescribeCaches()
        {
            Calls.Add("caches");
            return Caches;
        }

        public IReadOnlyList<ReloadDigestLine> DescribeAdoptedState()
        {
            Calls.Add("adopted");
            return Adopted;
        }

        public void RunNextFrame(Action action)
        {
            Calls.Add("next-frame");
            NextFrame.Add(action);
        }

        public void Retire() => Calls.Add("retire");
    }

    private sealed class Lifecycle : IHostLifecycle
    {
        public bool ModuleLoaded { get; init; }
        public string Environment => "Test";
        public bool ReloadEnabled { get; init; } = true;
    }

    private sealed class Services : IHostServices
    {
        private readonly Dictionary<Type, object> _services = new();

        public Services With<T>(T service)
        {
            _services[typeof(T)] = service;
            return this;
        }

        public object Resolve(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    private static (Services Services, RecordingHostClosures Closures, UnopenedRedisConnectionSource Redis, RecordingHostCallbacks Callbacks, RecordingHookControl Hooks) HostServices()
    {
        var closures = new RecordingHostClosures();
        var redis = new UnopenedRedisConnectionSource();
        var callbacks = new RecordingHostCallbacks();
        var hooks = new RecordingHookControl();
        var services = new Services()
            .With<global::NWN.Core.ICoreFunctionHandler>(closures)
            .With<IRedisConnectionSource>(redis)
            .With<IHostCallbacks>(callbacks)
            .With<IHookControl>(hooks)
            .With<INativeSinkBinding>(hooks);
        return (services, closures, redis, callbacks, hooks);
    }

    private static GenerationActivation Activation(IHostServices services) => new(1, null, null, null, services);

    [Test]
    public void Validation_runs_the_container_check_off_the_calling_thread()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var caller = Environment.CurrentManagedThreadId;

        // The calling thread stays blocked until validation is done, so the validation can only have run on another one.
        generation.ValidateAsync(CancellationToken.None).AsTask().Wait();

        boot.Calls.Should().Equal("validate");
        boot.ValidatedOnThread.Should().NotBe(0).And.NotBe(caller);
    }

    [Test]
    public async Task Activation_boots_the_server_with_the_hosts_handler_connection_and_directory_then_exposes_the_callbacks()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, closures, redis, callbacks, hooks) = HostServices();
        generation.ConfigureSourceDirectory("/nwn/home/dotnet/game");

        await generation.ValidateAsync(CancellationToken.None);
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        boot.Calls.Should().Equal("validate", "activate");
        boot.Boot.HostClosures.Should().BeSameAs(closures);
        boot.Boot.RedisConnection.Should().BeSameAs(redis);
        boot.Boot.HookControl.Should().BeSameAs(hooks);
        boot.Boot.GameDirectory.Should().Be("/nwn/home/dotnet/game");
        generation.Scripts.Should().BeOfType<HostedScriptDispatcher>();
        generation.MainLoop.Should().BeOfType<HostedMainLoop>();
        callbacks.Bound.Should().BeOfType<HostedSignalHandler>();
    }

    [Test]
    public async Task A_generation_activated_before_the_module_loads_is_a_boot_and_logs_nothing()
    {
        var boot = new RecordingBoot();
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false });

        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        boot.Calls.Should().Equal(new[] { "activate" }, "the engine signals the module load to generation 1 itself");
        log.Should().BeEmpty();
    }

    [Test]
    public async Task A_generation_activated_after_the_module_loaded_runs_the_reload_bootstrap_and_logs_its_digests()
    {
        var boot = new RecordingBoot
        {
            Rebuilt = new[]
            {
                new ReloadStepResult(Step(ReloadStepKind.Rerun, nameof(FixtureOwner.Rerun)), 12, null),
                new ReloadStepResult(Step(ReloadStepKind.Adopt, nameof(FixtureOwner.Adopt)), 3, null),
            }
        };
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, callbacks, hooks) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = true });

        await generation.ActivateAsync(new GenerationActivation(3, null, null, null, services), CancellationToken.None);

        boot.Calls.Should().Equal(new[] { "activate", "resume", "rebuild", "adopted", "next-frame" },
            "the engine signals the module load and runs the boot scripts only for generation 1, so a reload takes the signal, " +
            "reruns and adopts, and logs the adopted and world digests inside the pause and the cache digests a frame later");
        hooks.Calls.Should().Equal(new[] { "bind" });
        generation.Scripts.Should().BeOfType<HostedScriptDispatcher>();
        generation.MainLoop.Should().BeOfType<HostedMainLoop>();
        callbacks.Bound.Should().BeOfType<HostedSignalHandler>();
        log[0].Should().MatchRegex(@"^Generation 3 activated by a reload in \d+ ms: rebuilt the service container and the script dispatch, " +
                                   @"then the reload bootstrap reran=1 adopted=1 carried=0 failed=0 totalMs=15 slowest=.*FixtureOwner\.Rerun:12,.*FixtureOwner\.Adopt:3$");
        log.Should().Contain("SWLOR_RELOAD_DIGEST generation=3 phase=reload kind=world name=world.dockIds count=2 hash=00000000000000bb");

        boot.NextFrame.Should().ContainSingle();
        boot.NextFrame[0]();
        log.Should().Contain("SWLOR_RELOAD_DIGEST generation=3 phase=reload kind=cache name=Cache.Owner count=3 hash=00000000000000aa");
    }

    [Test]
    public async Task A_reload_with_hot_reload_off_rebuilds_but_logs_no_digest()
    {
        var boot = new RecordingBoot();
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = true, ReloadEnabled = false });

        await generation.ActivateAsync(Activation(services), CancellationToken.None);
        await generation.BeginQuiescenceAsync(CancellationToken.None);

        boot.Calls.Should().Equal("activate", "resume", "rebuild");
        log.Should().ContainSingle().Which.Should().StartWith("Generation 1 activated by a reload");
    }

    [Test]
    public async Task The_boot_of_a_server_with_hot_reload_on_logs_its_cache_digests_the_frame_after_the_module_loads()
    {
        var boot = new RecordingBoot();
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, callbacks, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false });
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        // The signal handler counts the managed entry through the combat recorder, a facade.
        using var container = new FacadeContainer();
        using var scope = container.Install();
        callbacks.Bound.OnSignal("SOME_OTHER_SIGNAL");
        boot.NextFrame.Should().BeEmpty();

        callbacks.Bound.OnSignal(HostedServerBoot.ModuleLoadFinishSignal);
        callbacks.Bound.OnSignal(HostedServerBoot.ModuleLoadFinishSignal);

        boot.Signals.Should().Equal("SOME_OTHER_SIGNAL", HostedServerBoot.ModuleLoadFinishSignal, HostedServerBoot.ModuleLoadFinishSignal);
        boot.NextFrame.Should().ContainSingle("the boot's digests are scheduled once");
        boot.NextFrame[0]();
        log.Should().Contain("SWLOR_RELOAD_DIGEST generation=1 phase=boot kind=cache name=Cache.Owner count=3 hash=00000000000000aa");
    }

    [Test]
    public async Task The_boot_of_a_server_with_hot_reload_off_schedules_no_digest()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot, _ => { });
        var (services, _, _, callbacks, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false, ReloadEnabled = false });
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        // The signal handler counts the managed entry through the combat recorder, a facade.
        using var container = new FacadeContainer();
        using var scope = container.Install();
        callbacks.Bound.OnSignal("SOME_OTHER_SIGNAL");

        boot.NextFrame.Should().BeEmpty();
        boot.Calls.Should().Equal("activate");
    }

    [Test]
    public async Task Quiescence_with_hot_reload_on_logs_the_adopted_digests_then_carries()
    {
        var boot = new RecordingBoot();
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false });
        await generation.ActivateAsync(new GenerationActivation(4, null, null, null, services), CancellationToken.None);

        await generation.BeginQuiescenceAsync(CancellationToken.None);

        boot.Calls.Should().Equal("activate", "adopted", "carry");
        log.Should().Contain("SWLOR_RELOAD_DIGEST generation=4 phase=quiesce kind=world name=world.dockIds count=2 hash=00000000000000bb");
        log.Should().Contain(line => line.StartsWith("Generation 4 quiesced for a reload: reran=0 adopted=0 carried=0 failed=0", StringComparison.Ordinal));
    }

    [Test]
    public async Task A_carry_that_fails_is_logged_and_the_quiescence_goes_on()
    {
        var boot = new RecordingBoot { CarryFailure = new InvalidOperationException("carry") };
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false });
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        var quiesce = async () => await generation.BeginQuiescenceAsync(CancellationToken.None);

        await quiesce.Should().NotThrowAsync();
        log.Should().Contain(line => line.StartsWith("Generation 1 could not carry its state for the reload: System.InvalidOperationException: carry", StringComparison.Ordinal));
    }

    [Test]
    public async Task A_digest_that_fails_is_logged_and_the_activation_goes_on()
    {
        var boot = new RecordingBoot { Adopted = null };
        var log = new List<string>();
        var generation = new GameGeneration(boot, log.Add);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = true });

        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        log.Should().Contain(line => line.StartsWith("SWLOR_RELOAD_DIGESTS generation=1 phase=reload failed: ", StringComparison.Ordinal));
        generation.Scripts.Should().NotBeNull();
    }

    private static ReloadStep Step(ReloadStepKind kind, string method)
    {
        return new ReloadStep(kind, ScriptName.OnModuleLoad, "Fixture.Handler", typeof(FixtureOwner).GetMethod(method), () => { });
    }

    private sealed class FixtureOwner
    {
        public static void Rerun() { }
        public static void Adopt() { }
    }

    [Test]
    public void A_generation_needs_a_log()
    {
        var create = () => new GameGeneration(new RecordingBoot(), null);

        create.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Nothing_is_exposed_before_activation()
    {
        var generation = new GameGeneration(new RecordingBoot());

        await generation.ValidateAsync(CancellationToken.None);

        generation.Scripts.Should().BeNull();
        generation.MainLoop.Should().BeNull();
    }

    [Test]
    public async Task Activation_binds_a_full_set_of_the_games_sinks_before_the_server_boots_and_enables_no_hook()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, _, _, _, hooks) = HostServices();

        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        hooks.Calls.Should().Equal(new[] { "bind" }, "the game's boot handlers enable the hooks later, at their own moments");
        hooks.Bound.Should().NotBeNull();
        hooks.Bound.VitalityHitPoints.Should().BeOfType<SWLOR.Game.Server.NativeSinks.VitalityHitPointsSink>();
        hooks.Bound.WeaponAttackAnimation.Should().BeOfType<SWLOR.Game.Server.NativeSinks.WeaponAttackAnimationSink>();
    }

    [Test]
    public async Task Dispose_unbinds_the_sinks_and_signals_stops_the_callbacks_and_lets_go_of_the_server_once()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, _, _, callbacks, hooks) = HostServices();
        await generation.ActivateAsync(Activation(services), CancellationToken.None);
        var bound = hooks.Bound;

        await generation.DisposeAsync();
        await generation.DisposeAsync();

        boot.Calls.Should().Equal("activate", "retire");
        hooks.Calls.Should().Equal("bind", "unbind");
        hooks.Bound.Should().BeNull("no hook references a type of the retired build");
        bound.Should().NotBeNull();
        callbacks.Calls.Should().Equal("bind", "unbind");
        callbacks.Bound.Should().BeNull();
        generation.Scripts.Should().BeNull();
        generation.MainLoop.Should().BeNull();
    }

    [Test]
    public async Task A_generation_that_never_activated_has_nothing_to_let_go_of()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);

        await generation.ValidateAsync(CancellationToken.None);
        await generation.DisposeAsync();

        boot.Calls.Should().Equal("validate");
    }

    [Test]
    public async Task An_activation_that_fails_part_way_is_still_let_go_of()
    {
        var boot = new RecordingBoot { ActivationFailure = new InvalidOperationException("container") };
        var generation = new GameGeneration(boot);
        var (services, _, _, callbacks, hooks) = HostServices();

        var activate = async () => await generation.ActivateAsync(Activation(services), CancellationToken.None);
        await activate.Should().ThrowAsync<InvalidOperationException>().WithMessage("container");
        await generation.DisposeAsync();

        boot.Calls.Should().Equal("activate", "retire");
        hooks.Calls.Should().Equal("bind", "unbind");
        hooks.Bound.Should().BeNull();
        callbacks.Calls.Should().BeEmpty();
        generation.Scripts.Should().BeNull();
    }

    [TestCase(typeof(global::NWN.Core.ICoreFunctionHandler))]
    [TestCase(typeof(IRedisConnectionSource))]
    [TestCase(typeof(IHostCallbacks))]
    [TestCase(typeof(IHookControl))]
    [TestCase(typeof(INativeSinkBinding))]
    public async Task Activation_without_a_host_service_fails_before_the_server_boots(Type missing)
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (all, closures, redis, callbacks, hooks) = HostServices();
        var services = new Services();
        if (missing != typeof(global::NWN.Core.ICoreFunctionHandler)) services.With<global::NWN.Core.ICoreFunctionHandler>(closures);
        if (missing != typeof(IRedisConnectionSource)) services.With<IRedisConnectionSource>(redis);
        if (missing != typeof(IHostCallbacks)) services.With<IHostCallbacks>(callbacks);
        if (missing != typeof(IHookControl)) services.With<IHookControl>(hooks);
        if (missing != typeof(INativeSinkBinding)) services.With<INativeSinkBinding>(hooks);

        var activate = async () => await generation.ActivateAsync(Activation(services), CancellationToken.None);

        await activate.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*no {missing.Name}*");
        boot.Calls.Should().BeEmpty();
        hooks.Calls.Should().BeEmpty("nothing is bound when the generation cannot boot");
        all.Should().NotBeNull();
    }

    [Test]
    public async Task Activation_needs_the_activation_and_the_host_services()
    {
        var generation = new GameGeneration(new RecordingBoot());

        var withoutActivation = async () => await generation.ActivateAsync(null, CancellationToken.None);
        var withoutServices = async () => await generation.ActivateAsync(Activation(null), CancellationToken.None);
        var withoutBoot = () => new GameGeneration(null);

        await withoutActivation.Should().ThrowAsync<ArgumentNullException>();
        await withoutServices.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no services*");
        withoutBoot.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Quiescence_and_drain_have_nothing_to_do_while_reload_is_off()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, _, _, _, _) = HostServices();
        services.With<IHostLifecycle>(new Lifecycle { ModuleLoaded = false, ReloadEnabled = false });

        await generation.BeginQuiescenceAsync(CancellationToken.None);
        await generation.ActivateAsync(Activation(services), CancellationToken.None);
        await generation.BeginQuiescenceAsync(CancellationToken.None);
        await generation.DrainAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        boot.Calls.Should().Equal("activate");
    }

    [Test]
    public void The_build_id_is_the_game_assemblys_module_version()
    {
        new GameGeneration().BuildId.Should().Be(typeof(GameGeneration).Assembly.ManifestModule.ModuleVersionId.ToString("N"));
    }

    [Test]
    public void The_required_schema_is_the_newest_server_migration_of_the_build()
    {
        var expected = typeof(GameGeneration).Assembly.GetTypes()
            .Where(type => typeof(IServerMigration).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => ((IServerMigration)Activator.CreateInstance(type)).Version)
            .DefaultIfEmpty(MigrationService.RetiredServerMigrationVersion)
            .Max();

        new GameGeneration().RequiredSchemaVersion.Should().Be(expected).And.BeGreaterThanOrEqualTo(MigrationService.RetiredServerMigrationVersion);
    }

    [Test]
    public void A_build_without_server_migrations_requires_the_retired_version()
    {
        GameGeneration.NewestServerMigration(new[] { typeof(string) }).Should().Be(MigrationService.RetiredServerMigrationVersion);
        GameGeneration.NewestServerMigration(new[] { typeof(FixtureMigration), typeof(IServerMigration) }).Should().Be(41);
    }

    [Test]
    public void The_game_assembly_holds_exactly_one_generation_with_a_public_parameterless_constructor()
    {
        var generations = typeof(GameGeneration).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IGameplayGeneration).IsAssignableFrom(type))
            .ToList();

        generations.Should().Equal(typeof(GameGeneration));
        typeof(GameGeneration).GetConstructor(Type.EmptyTypes).Should().NotBeNull("the host creates the generation with Activator.CreateInstance");
    }

    private sealed class FixtureMigration : IServerMigration
    {
        public int Version => 41;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate() { }
    }
}
