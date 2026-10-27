using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves the rules the container is built by: once, validated, with its singletons created at build time, and loud
/// before it exists.
/// </summary>
[TestFixture]
public class ContainerHostTests
{
    public interface IGreeter
    {
        string Greet();
    }

    public sealed class Greeter : IGreeter
    {
        public string Greet() => "hello";
    }

    public interface IPrefix
    {
    }

    public sealed class NeedsPrefix : IGreeter
    {
        public NeedsPrefix(IPrefix prefix)
        {
        }

        public string Greet() => "never";
    }

    public sealed class ThrowsInConstructor : IGreeter
    {
        public ThrowsInConstructor() => throw new InvalidOperationException("constructor failed");

        public string Greet() => "never";
    }

    public sealed class CountsConstructions : IGreeter
    {
        public static int Constructed;

        public CountsConstructions() => Constructed++;

        public string Greet() => "counted";
    }

    [Test]
    public void A_host_that_has_not_been_built_fails_loudly_when_asked_for_its_container()
    {
        var host = new ContainerHost();

        host.IsBuilt.Should().BeFalse();
        var asking = () => host.Provider;
        asking.Should().Throw<ContainerNotBuiltException>().WithMessage("*has not been built yet*BuildServiceContainer*");
    }

    [Test]
    public void Build_makes_the_container_available_and_reports_the_registrations()
    {
        var host = new ContainerHost();

        var registrations = host.Build(services => services.AddSingleton<IGreeter, Greeter>());

        registrations.Should().Be(1);
        host.IsBuilt.Should().BeTrue();
        host.Provider.GetRequiredService<IGreeter>().Greet().Should().Be("hello");
    }

    [Test]
    public void Build_is_allowed_only_once_and_the_first_container_stays()
    {
        var host = new ContainerHost();
        host.Build(services => services.AddSingleton<IGreeter, Greeter>());
        var first = host.Provider;

        var second = () => host.Build(services => { });

        second.Should().Throw<ContainerNotBuiltException>().WithMessage("*already built*");
        host.Provider.Should().BeSameAs(first);
    }

    [Test]
    public void Every_service_is_one_instance_for_the_life_of_the_container()
    {
        var host = new ContainerHost();
        host.Build(services => services.AddSingleton<IGreeter, Greeter>());

        host.Provider.GetRequiredService<IGreeter>().Should().BeSameAs(host.Provider.GetRequiredService<IGreeter>());
    }

    [Test]
    public void A_registration_with_a_missing_dependency_fails_the_build_not_the_first_use()
    {
        var host = new ContainerHost();

        var build = () => host.Build(services => services.AddSingleton<IGreeter, NeedsPrefix>());

        build.Should().Throw<AggregateException>();
        host.IsBuilt.Should().BeFalse("a failed build leaves no container behind");
    }

    [Test]
    public void A_singleton_depending_on_a_scoped_service_fails_the_build()
    {
        var host = new ContainerHost();

        var build = () => host.Build(services =>
        {
            services.AddScoped<IPrefix, ScopedPrefix>();
            services.AddSingleton<IGreeter, NeedsPrefix>();
        });

        build.Should().Throw<Exception>();
        host.IsBuilt.Should().BeFalse();
    }

    [Test]
    public void Singletons_are_created_when_the_container_is_built()
    {
        CountsConstructions.Constructed = 0;
        var host = new ContainerHost();

        host.Build(services => services.AddSingleton<IGreeter, CountsConstructions>());

        CountsConstructions.Constructed.Should().Be(1, "a constructor that throws must stop the boot, not the first handler that needs the service");
    }

    [Test]
    public void A_constructor_that_throws_stops_the_build()
    {
        var host = new ContainerHost();

        var build = () => host.Build(services => services.AddSingleton<IGreeter, ThrowsInConstructor>());

        build.Should().Throw<InvalidOperationException>().WithMessage("constructor failed");
        host.IsBuilt.Should().BeFalse();
    }

    [Test]
    public void The_container_options_validate_scopes()
    {
        using var provider = ContainerHost.Create(new ServiceCollection().AddScoped<IPrefix, ScopedPrefix>());

        var resolveScopedFromRoot = () => provider.GetRequiredService<IPrefix>();

        resolveScopedFromRoot.Should().Throw<InvalidOperationException>("a scoped service cannot be resolved from the root container");
    }

    [Test]
    public void Create_rejects_a_missing_collection_and_Build_a_missing_registration()
    {
        var create = () => ContainerHost.Create(null);
        var build = () => new ContainerHost().Build(null);

        create.Should().Throw<ArgumentNullException>();
        build.Should().Throw<ArgumentNullException>();
    }

    public sealed class ScopedPrefix : IPrefix
    {
    }
}
