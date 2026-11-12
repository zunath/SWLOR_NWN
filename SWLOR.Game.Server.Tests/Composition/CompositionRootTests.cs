using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves the server's container holder: the accessor fails loudly before the container is built, and a test
/// container installed for one async flow reaches GameServices and goes away when disposed.
/// </summary>
[TestFixture]
public class CompositionRootTests
{
    public interface IProbe
    {
        string Name { get; }
    }

    private sealed class Probe : IProbe
    {
        public Probe(string name) => Name = name;

        public string Name { get; }
    }

    [Test]
    public void The_server_container_is_not_built_by_the_test_process()
    {
        // Only ServerBootstrapper builds it; if this fails, a test built the process-wide container and the
        // "not built" checks below are meaningless.
        CompositionRoot.IsBuilt.Should().BeFalse();
    }

    [Test]
    public void The_host_entry_builds_the_same_registrations_with_the_hosts_redis_connection()
    {
        var redis = new SWLOR.Game.Server.Tests.Support.UnopenedRedisConnectionSource();
        var hosted = new ContainerHost();
        var native = new ContainerHost();

        var hostedCount = CompositionRoot.Build(hosted, redis);
        var nativeCount = CompositionRoot.Build(native);

        hostedCount.Should().Be(nativeCount, "the host's connection replaces the server's own; nothing is added");
        hosted.Resolve<SWLOR.Game.Contracts.IRedisConnectionSource>().Should().BeSameAs(redis);
        native.Resolve<SWLOR.Game.Contracts.IRedisConnectionSource>().Should().BeOfType<SWLOR.Game.Server.Domain.Foundation.RedisConnectionSource>();
        CompositionRoot.IsBuilt.Should().BeFalse("the server's own container is untouched");
    }

    [Test]
    public void Validating_the_server_services_builds_and_discards_a_container_of_its_own()
    {
        var count = CompositionRoot.ValidateServerServices();

        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        count.Should().Be(services.Count);
        CompositionRoot.IsBuilt.Should().BeFalse("the server's own container is untouched");
    }

    [Test]
    public void The_host_entry_build_needs_a_container_and_a_connection()
    {
        var withoutHost = () => CompositionRoot.Build(null, new SWLOR.Game.Server.Tests.Support.UnopenedRedisConnectionSource());
        var withoutConnection = () => CompositionRoot.Build(new ContainerHost(), null);
        var registerWithoutConnection = () => CompositionRoot.RegisterHostedServices(new ServiceCollection(), null);

        withoutHost.Should().Throw<ArgumentNullException>();
        withoutConnection.Should().Throw<ArgumentNullException>();
        registerWithoutConnection.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Resolving_before_the_container_is_built_fails_loudly()
    {
        Assume.That(CompositionRoot.IsBuilt, Is.False);

        var resolve = () => ResolveThroughTheFacadeAccessor();

        resolve.Should().Throw<ContainerNotBuiltException>().WithMessage("*has not been built yet*");
    }

    [Test]
    public void An_installed_container_is_what_the_facade_accessor_resolves_from()
    {
        using var provider = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("installed")));

        using (CompositionRoot.InstallForTests(provider))
        {
            ResolveThroughTheFacadeAccessor().Name.Should().Be("installed");
        }
    }

    [Test]
    public void Disposing_the_scope_removes_the_installed_container()
    {
        Assume.That(CompositionRoot.IsBuilt, Is.False);
        using var provider = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("installed")));
        var scope = CompositionRoot.InstallForTests(provider);

        scope.Dispose();
        scope.Dispose();

        var resolve = () => ResolveThroughTheFacadeAccessor();
        resolve.Should().Throw<ContainerNotBuiltException>();
    }

    [Test]
    public void Scopes_nest_and_each_restores_the_previous_container()
    {
        using var outer = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("outer")));
        using var inner = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("inner")));

        using (CompositionRoot.InstallForTests(outer))
        {
            using (CompositionRoot.InstallForTests(inner))
            {
                ResolveThroughTheFacadeAccessor().Name.Should().Be("inner");
            }

            ResolveThroughTheFacadeAccessor().Name.Should().Be("outer");
        }
    }

    [Test]
    public async Task An_installed_container_is_seen_by_tasks_started_inside_the_scope_only()
    {
        using var first = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("first")));
        using var second = ContainerHost.Create(new ServiceCollection().AddSingleton<IProbe>(new Probe("second")));
        var gate = new TaskCompletionSource();

        async Task<string> ResolveAfterGate(IServiceProvider provider)
        {
            using (CompositionRoot.InstallForTests(provider))
            {
                await gate.Task;
                return ResolveThroughTheFacadeAccessor().Name;
            }
        }

        var one = Task.Run(() => ResolveAfterGate(first));
        var two = Task.Run(() => ResolveAfterGate(second));
        gate.SetResult();

        (await one).Should().Be("first");
        (await two).Should().Be("second");
    }

    [Test]
    public void Installing_a_missing_container_is_rejected()
    {
        var install = () => CompositionRoot.InstallForTests(null);

        install.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void The_server_registrations_build_a_valid_container()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);

        using var provider = ContainerHost.Create(services);

        provider.Should().NotBeNull();
    }

    // FacadeAccessor is internal to the game assembly (only Composition may use it), so the test reaches it by reflection.
    // This is the call every GameServices property makes.
    private static IProbe ResolveThroughTheFacadeAccessor()
    {
        var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!;
        var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IProbe));
        try
        {
            return (IProbe)resolve.Invoke(null, null)!;
        }
        catch (TargetInvocationException exception)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
            throw;
        }
    }
}
