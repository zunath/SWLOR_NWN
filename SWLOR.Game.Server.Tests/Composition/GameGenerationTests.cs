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

        public void Retire() => Calls.Add("retire");
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

    private static (Services Services, RecordingHostClosures Closures, UnopenedRedisConnectionSource Redis, RecordingHostCallbacks Callbacks) HostServices()
    {
        var closures = new RecordingHostClosures();
        var redis = new UnopenedRedisConnectionSource();
        var callbacks = new RecordingHostCallbacks();
        var services = new Services()
            .With<global::NWN.Core.ICoreFunctionHandler>(closures)
            .With<IRedisConnectionSource>(redis)
            .With<IHostCallbacks>(callbacks);
        return (services, closures, redis, callbacks);
    }

    private static GenerationActivation Activation(IHostServices services) => new(1, null, null, null, services);

    [Test]
    public async Task Validation_runs_the_container_check_off_the_calling_thread()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);

        await generation.ValidateAsync(CancellationToken.None);

        boot.Calls.Should().Equal("validate");
        boot.ValidatedOnThread.Should().NotBe(0);
    }

    [Test]
    public async Task Activation_boots_the_server_with_the_hosts_handler_connection_and_directory_then_exposes_the_callbacks()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, closures, redis, callbacks) = HostServices();
        generation.ConfigureSourceDirectory("/nwn/home/dotnet/game");

        await generation.ValidateAsync(CancellationToken.None);
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        boot.Calls.Should().Equal("validate", "activate");
        boot.Boot.HostClosures.Should().BeSameAs(closures);
        boot.Boot.RedisConnection.Should().BeSameAs(redis);
        boot.Boot.GameDirectory.Should().Be("/nwn/home/dotnet/game");
        generation.Scripts.Should().BeOfType<HostedScriptDispatcher>();
        generation.MainLoop.Should().BeOfType<HostedMainLoop>();
        callbacks.Bound.Should().BeOfType<HostedSignalHandler>();
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
    public async Task Dispose_unbinds_the_signals_stops_the_callbacks_and_lets_go_of_the_server_once()
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (services, _, _, callbacks) = HostServices();
        await generation.ActivateAsync(Activation(services), CancellationToken.None);

        await generation.DisposeAsync();
        await generation.DisposeAsync();

        boot.Calls.Should().Equal("activate", "retire");
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
        var (services, _, _, callbacks) = HostServices();

        var activate = async () => await generation.ActivateAsync(Activation(services), CancellationToken.None);
        await activate.Should().ThrowAsync<InvalidOperationException>().WithMessage("container");
        await generation.DisposeAsync();

        boot.Calls.Should().Equal("activate", "retire");
        callbacks.Calls.Should().BeEmpty();
        generation.Scripts.Should().BeNull();
    }

    [TestCase(typeof(global::NWN.Core.ICoreFunctionHandler))]
    [TestCase(typeof(IRedisConnectionSource))]
    [TestCase(typeof(IHostCallbacks))]
    public async Task Activation_without_a_host_service_fails_before_the_server_boots(Type missing)
    {
        var boot = new RecordingBoot();
        var generation = new GameGeneration(boot);
        var (all, closures, redis, callbacks) = HostServices();
        var services = new Services();
        if (missing != typeof(global::NWN.Core.ICoreFunctionHandler)) services.With<global::NWN.Core.ICoreFunctionHandler>(closures);
        if (missing != typeof(IRedisConnectionSource)) services.With<IRedisConnectionSource>(redis);
        if (missing != typeof(IHostCallbacks)) services.With<IHostCallbacks>(callbacks);

        var activate = async () => await generation.ActivateAsync(Activation(services), CancellationToken.None);

        await activate.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*no {missing.Name}*");
        boot.Calls.Should().BeEmpty();
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
        var generation = new GameGeneration(new RecordingBoot());

        await generation.BeginQuiescenceAsync(CancellationToken.None);
        await generation.DrainAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
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
