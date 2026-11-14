using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using StackExchange.Redis;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The setup of every integration test: an empty database, the server's own container built from
/// <see cref="CompositionRoot.RegisterServerServices"/> with the real database service, and a recording fake engine
/// behind the static NWScript wrappers. Game code that reaches a service through its static facade resolves from this
/// container for the length of the test.
/// </summary>
[CancelAfter(180000)]
public abstract class IntegrationTestBase
{
    private ServiceProvider _provider = null!;
    private IDisposable _containerScope = null!;
    private FakeEngineSet _engineSet = null!;

    protected IntegrationDBService Db => IntegrationEnvironment.Db;

    protected IDatabase Redis => IntegrationEnvironment.Database;

    /// <summary>The fake engine. A test configures what the engine answers and asserts on the calls.</summary>
    protected FakeNWScript Engine => _engineSet.Script;

    /// <summary>The fakes of every NWNX plugin, beside <see cref="Engine"/>.</summary>
    protected FakeEngineSet Fakes => _engineSet;

    protected T Service<T>()
        where T : notnull => _provider.GetRequiredService<T>();

    protected const uint Hero = 1001;
    protected const string HeroId = "hero-uuid-0001";

    /// <summary>Makes the engine report <paramref name="player"/> as a living player character with the given UUID, entering the module.</summary>
    protected void ConfigurePlayer(uint player = Hero, string playerId = HeroId)
    {
        Engine.SetReturn("GetIsPC", true);
        Engine.SetReturn("GetIsDM", false);
        Engine.SetReturn("GetIsDead", false);
        Engine.SetReturn("GetCurrentHitPoints", 100);
        Engine.SetReturn("GetEnteringObject", player);
        Engine.SetReturn("GetObjectUUID", playerId);
        Engine.SetReturn("GetName", "Test Hero");
        // Only the player is a valid object: engine loops that walk "the next item" end on the first invalid answer.
        Engine.SetReturnHandler("GetIsObjectValid", arguments => arguments.Count > 0 && Equals(arguments[0], player));
    }

    /// <summary>The messages sent to players, in order.</summary>
    protected IReadOnlyList<string> Messages() =>
        Engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>Hook for a test that replaces some registrations (a service it wants to subclass) before the container is built.</summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    [SetUp]
    public void BeginIntegrationTest()
    {
        IntegrationEnvironment.Wipe();

        _engineSet = new FakeEngineSet();

        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        services.Replace(ServiceDescriptor.Singleton<IDBService>(Db));
        services.Replace(ServiceDescriptor.Singleton<IMessagingService, UnconcealedMessagingService>());
        services.Replace(ServiceDescriptor.Singleton<IMigrationService>(provider => new IntegrationMigrationService(provider.GetRequiredService<IDBService>())));
        ConfigureServices(services);
        _provider = ContainerHost.Create(services);
        _containerScope = CompositionRoot.InstallForTests(_provider);
    }

    [TearDown]
    public void EndIntegrationTest()
    {
        _containerScope.Dispose();
        _provider.Dispose();
        _engineSet.Dispose();
    }
}
