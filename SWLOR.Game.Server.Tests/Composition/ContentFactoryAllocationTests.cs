using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Tests.Composition.ContentSamples;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// What a <c>Create</c> costs (MaintainabilityRefactorContentInjection.md, "What it costs"): the object and nothing else, no argument
/// array, no boxing, no lookup per service. Pinned against the constructor called directly with the same services.
/// </summary>
[TestFixture]
public class ContentFactoryAllocationTests
{
    private const int Calls = 1_000;

    private FacadeContainer _container = null!;
    private object? _sink;

    private IContentFactory Content => _container.Content;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer(services => services.AddSingleton<SampleSingleton>());

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    private long Allocated(Func<object> create)
    {
        for (var i = 0; i < 100; i++)
            _sink = create();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            _sink = create();
        return (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;
    }

    [Test]
    public void Create_with_a_value_allocates_exactly_what_the_constructor_allocates()
    {
        var party = _container.Get<IPartyService>();
        var singleton = _container.Get<SampleSingleton>();

        var direct = Allocated(() => new AllocationContent(party, singleton, 5));
        var made = Allocated(() => Content.Create<AllocationContent, int>(5));

        direct.Should().BeGreaterThan(0);
        made.Should().Be(direct, "the factory allocates the object and nothing else");
        TestContext.Out.WriteLine($"AllocationContent (2 services, 1 value): {made} bytes per Create, {direct} per new");
    }

    [Test]
    public void Create_without_values_and_by_type_allocate_exactly_what_the_constructor_allocates()
    {
        var party = _container.Get<IPartyService>();
        var singleton = _container.Get<SampleSingleton>();

        var direct = Allocated(() => new SampleContent(party, singleton));
        Allocated(() => Content.Create<SampleContent>()).Should().Be(direct);
        Allocated(() => Content.Create<object>(typeof(SampleContent))).Should().Be(direct);
    }

    [Test]
    public void Create_with_several_values_boxes_none()
    {
        var party = _container.Get<IPartyService>();
        var singleton = _container.Get<SampleSingleton>();

        var direct = Allocated(() => new SampleContent(party, singleton, 3, "label", SampleKind.First, 4));
        Allocated(() => Content.Create<SampleContent, int, string, SampleKind, int?>(3, "label", SampleKind.First, 4)).Should().Be(direct);
    }

    /// <summary>
    /// Measurement, not a check: ns per call of the constructor called directly, of <c>Create</c> with a value through the interface,
    /// and of <c>Create</c> by type. Explicit so the default run skips it; run it alone in Release:
    /// <c>dotnet test SWLOR.Game.Server.Tests -c Release --no-build --filter "FullyQualifiedName~ContentFactoryAllocationTests.Measure" --logger "console;verbosity=detailed"</c>
    /// </summary>
    [Test, Explicit("Micro-benchmark. Run on its own in Release.")]
    public void Measure_create_cost()
    {
        const int iterations = 20_000_000;
        var party = _container.Get<IPartyService>();
        var singleton = _container.Get<SampleSingleton>();
        var content = Content;

        double Time(Func<object> create)
        {
            for (var i = 0; i < 1_000_000; i++) _sink = create();
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++) _sink = create();
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds * 1_000_000 / iterations;
        }

        var directNs = Time(() => new AllocationContent(party, singleton, 5));
        var createNs = Time(() => content.Create<AllocationContent, int>(5));
        var directValuesFreeNs = Time(() => new SampleContent(party, singleton));
        var byTypeNs = Time(() => content.Create<object>(typeof(SampleContent)));
        var bytes = Allocated(() => content.Create<AllocationContent, int>(5));

        TestContext.Out.WriteLine($"new AllocationContent(...): {directNs:F2} ns/call");
        TestContext.Out.WriteLine($"Content.Create<AllocationContent, int>(5): {createNs:F2} ns/call, {bytes} bytes/call");
        TestContext.Out.WriteLine($"new SampleContent(party, singleton): {directValuesFreeNs:F2} ns/call");
        TestContext.Out.WriteLine($"Content.Create<object>(typeof(SampleContent)) (by type): {byTypeNs:F2} ns/call");
        _sink.Should().NotBeNull();
    }
}
