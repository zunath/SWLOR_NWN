using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// A converted class takes the interfaces it calls through its constructor; where two classes need each other, one of
/// them takes <c>Lazy&lt;IXService&gt;</c>, which the container gives from one open-generic registration (decision D3). This
/// proves the registration resolves the singleton on first use, and that the server uses <c>Lazy&lt;&gt;</c> only to break a
/// real constructor cycle.
/// </summary>
[TestFixture]
public class LazyDependencyCycleTests
{
    public interface IFirst
    {
        Lazy<ISecond> Second { get; }
    }

    public interface ISecond
    {
        IFirst First { get; }
    }

    public interface IThird
    {
    }

    public sealed class First : IFirst
    {
        public First(Lazy<ISecond> second) => Second = second;

        public Lazy<ISecond> Second { get; }
    }

    public sealed class Second : ISecond
    {
        public Second(IFirst first) => First = first;

        public IFirst First { get; }
    }

    public sealed class NeedlesslyLazy : IThird
    {
        public NeedlesslyLazy(Lazy<IFirst> first)
        {
        }
    }

    [Test]
    public void Every_lazy_constructor_parameter_of_a_server_service_breaks_a_real_cycle()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);

        LazyDependencyCycles.NeedlessLazies(services).Should().BeEmpty(
            "a Lazy<IXService> parameter is allowed only where taking IXService directly would make a constructor cycle");
    }

    [Test]
    public void A_lazy_parameter_that_breaks_a_cycle_is_accepted_and_one_that_breaks_none_is_reported()
    {
        var services = new ServiceCollection()
            .AddSingleton<IFirst, First>()
            .AddSingleton<ISecond, Second>()
            .AddSingleton<IThird, NeedlesslyLazy>();

        LazyDependencyCycles.NeedlessLazies(services).Should().ContainSingle()
            .Which.Should().StartWith("NeedlesslyLazy takes Lazy<IFirst>, but First never reaches NeedlesslyLazy");
    }

    [Test]
    public void A_lazy_parameter_of_a_service_the_container_does_not_register_is_reported()
    {
        var services = new ServiceCollection().AddSingleton<IFirst, First>();

        LazyDependencyCycles.NeedlessLazies(services).Should().ContainSingle()
            .Which.Should().Be("First takes Lazy<ISecond>, which the container does not register.");
    }

    [Test]
    public void The_server_registrations_give_a_lazy_that_resolves_the_singleton_on_first_use_and_breaks_the_cycle()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        services.AddSingleton<IFirst, First>().AddSingleton<ISecond, Second>();

        using var provider = ContainerHost.Create(services);
        var first = provider.GetRequiredService<IFirst>();
        var second = provider.GetRequiredService<ISecond>();

        second.First.Should().BeSameAs(first);
        first.Second.Value.Should().BeSameAs(second, "the lazy resolves the container's singleton");
        first.Second.Value.Should().BeSameAs(first.Second.Value);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(Lazy<>))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
