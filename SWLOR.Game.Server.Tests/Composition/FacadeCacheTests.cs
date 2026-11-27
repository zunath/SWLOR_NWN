using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves the per-service cache behind the accessor: it returns the container's singleton, never an instance of
/// a container that is no longer current, survives container swaps, and is safe for concurrent first resolves.
/// </summary>
[TestFixture]
public class FacadeCacheTests
{
    public interface ICacheProbe
    {
        int Value { get; }
    }

    private sealed class CacheProbe : ICacheProbe
    {
        public CacheProbe(int value) => Value = value;

        public int Value { get; }
    }

    // FacadeAccessor is internal to the game assembly, so the tests reach it through a delegate built once by reflection.
    private static readonly Func<ICacheProbe> ResolveProbe = (Func<ICacheProbe>)typeof(CompositionRoot).Assembly
        .GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!
        .GetMethod("Resolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
        .MakeGenericMethod(typeof(ICacheProbe))
        .CreateDelegate(typeof(Func<ICacheProbe>));

    private static ICacheProbe Resolve() => ResolveProbe();

    private static ServiceProvider Container(int value)
    {
        return ContainerHost.Create(new ServiceCollection().AddSingleton<ICacheProbe>(new CacheProbe(value)));
    }

    [Test]
    public void Repeated_resolves_return_the_same_instance()
    {
        using var provider = Container(1);
        using (CompositionRoot.InstallForTests(provider))
        {
            var first = Resolve();

            Resolve().Should().BeSameAs(first);
            first.Should().BeSameAs(provider.GetRequiredService<ICacheProbe>());
        }
    }

    [Test]
    public void A_different_container_is_never_served_the_cached_instance_of_the_old_one()
    {
        using var one = Container(1);
        using var two = Container(2);

        using (CompositionRoot.InstallForTests(one))
        {
            Resolve().Value.Should().Be(1);
        }

        using (CompositionRoot.InstallForTests(two))
        {
            Resolve().Value.Should().Be(2);
        }

        using (CompositionRoot.InstallForTests(one))
        {
            Resolve().Value.Should().Be(1);
        }
    }

    [Test]
    public void A_cached_instance_does_not_outlive_its_installed_scope()
    {
        Assume.That(CompositionRoot.IsBuilt, Is.False);
        using var provider = Container(1);
        using (CompositionRoot.InstallForTests(provider))
        {
            Resolve();
        }

        var resolve = () => Resolve();

        resolve.Should().Throw<ContainerNotBuiltException>();
    }

    [Test]
    public void Nested_scopes_resolve_from_the_innermost_container_and_restore_the_outer_one()
    {
        using var outer = Container(1);
        using var inner = Container(2);

        using (CompositionRoot.InstallForTests(outer))
        {
            Resolve().Value.Should().Be(1);
            using (CompositionRoot.InstallForTests(inner))
            {
                Resolve().Value.Should().Be(2);
            }

            Resolve().Value.Should().Be(1);
        }
    }

    [Test]
    public void Facade_containers_with_different_registrations_swap_cleanly_between_fixtures()
    {
        using var first = new FacadeContainer(services => services.AddSingleton<ICacheProbe>(new CacheProbe(7)));
        using var second = new FacadeContainer(services => services.AddSingleton<ICacheProbe>(new CacheProbe(8)));

        for (var round = 0; round < 3; round++)
        {
            using (first.Install())
            {
                Resolve().Value.Should().Be(7);
            }

            using (second.Install())
            {
                Resolve().Value.Should().Be(8);
            }
        }
    }

    [Test]
    public void Concurrent_first_resolves_all_get_the_containers_singleton()
    {
        for (var round = 0; round < 50; round++)
        {
            using var provider = Container(round);
            var expected = provider.GetRequiredService<ICacheProbe>();
            var results = new ICacheProbe[16];
            using var start = new ManualResetEventSlim(false);

            var tasks = Enumerable.Range(0, results.Length)
                .Select(index => Task.Run(() =>
                {
                    using (CompositionRoot.InstallForTests(provider))
                    {
                        start.Wait();
                        results[index] = Resolve();
                    }
                }))
                .ToArray();
            start.Set();
            Task.WaitAll(tasks);

            results.Should().OnlyContain(result => ReferenceEquals(result, expected));
        }
    }

    [Test]
    public void Parallel_flows_with_different_containers_each_see_their_own()
    {
        using var one = Container(1);
        using var two = Container(2);

        var failures = 0;
        Parallel.For(0, 200, index =>
        {
            var provider = index % 2 == 0 ? one : two;
            using (CompositionRoot.InstallForTests(provider))
            {
                for (var i = 0; i < 200; i++)
                {
                    if (Resolve().Value != (index % 2 == 0 ? 1 : 2))
                        Interlocked.Increment(ref failures);
                }
            }
        });

        failures.Should().Be(0);
    }

    /// <summary>
    /// Measurement, not a check: ns per call of a real service member (<c>GameServices.Bank.IsFull</c>: resolve the instance, call one
    /// member), once through an installed test container and once through the server's own container. Explicit so the
    /// default run skips it, and it builds the process-wide container, so run it alone in Release:
    /// <c>dotnet test SWLOR.Game.Server.Tests -c Release --no-build --filter "FullyQualifiedName~FacadeCacheTests.Measure" --logger "console;verbosity=detailed"</c>
    /// </summary>
    [Test, Explicit("Micro-benchmark; builds the server container. Run on its own in Release.")]
    public void Measure_facade_call_cost()
    {
        const int iterations = 50_000_000;
        long sink = 0;

        double Time(Func<bool> body)
        {
            for (var i = 0; i < 2_000_000; i++) sink += body() ? 1 : 0;
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++) sink += body() ? 1 : 0;
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds * 1_000_000 / iterations;
        }

        using var container = new FacadeContainer();
        var bank = container.Get<IBankService>();
        double directNs, installedNs;
        using (container.Install())
        {
            directNs = Time(() => bank.IsFull(1));
            installedNs = Time(() => Bank.IsFull(1));
        }

        CompositionRoot.Build(CompositionRoot.ServerHost);
        var serverNs = Time(() => Bank.IsFull(1));

        TestContext.Out.WriteLine($"direct instance call: {directNs:F2} ns/call");
        TestContext.Out.WriteLine($"GameServices, installed test container (AsyncLocal lookup): {installedNs:F2} ns/call");
        TestContext.Out.WriteLine($"GameServices, server container: {serverNs:F2} ns/call");
        sink.Should().BeGreaterThan(-1);
    }
}
