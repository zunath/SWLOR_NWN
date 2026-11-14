using System.Diagnostics;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One run of every Phase 5 debt measure over the solution's source and the compiled server.</summary>
internal sealed class DebtScan
{
    private DebtScan(IReadOnlyList<DebtMeasure> measures, IReadOnlyList<string> timings)
    {
        Measures = measures;
        Timings = timings;
    }

    /// <summary>The eight measures, in the plan's order.</summary>
    public IReadOnlyList<DebtMeasure> Measures { get; }

    /// <summary>How long each stage took, for the test output.</summary>
    public IReadOnlyList<string> Timings { get; }

    public DebtMeasure Measure(string fileName) => Measures.Single(measure => measure.FileName == fileName);

    public static DebtScan Run(
        SourceSnapshot snapshot,
        ExemptionList exemptions,
        TypeReferenceGraph graph,
        DomainDependencies dependencies,
        AssemblyFacts facts)
    {
        var timings = new List<string>();
        var clock = Stopwatch.StartNew();
        T Timed<T>(string stage, Func<T> run)
        {
            clock.Restart();
            var result = run();
            timings.Add($"{stage}: {clock.Elapsed.TotalSeconds:F1} s");
            return result;
        }

        var sites = Timed("lookup (bound in every project)", () => LookupScanner.Scan(snapshot));
        var serverSites = sites.Where(site => site.Project == SourceSnapshot.ServerProjectName).ToList();
        var types = Timed("type sizes", () => SizeScanner.Types(snapshot.Server, exemptions));
        var members = Timed("member sizes", () => SizeScanner.Members(snapshot.Server, exemptions, DebtLimits.MethodLines, DebtLimits.LayoutMethodLines));
        var constructorServices = Timed("constructor services", () => ServiceScanner.ConstructorServices(snapshot, ServiceScanner.ServiceTypes(snapshot)));
        var providerUses = Timed("IServiceProvider", () => ServiceScanner.ServiceProviderUses(snapshot, DebtMeasures.CompositionFolder));
        var content = Timed("logic to content", () => LogicToContentScanner.Scan(snapshot, dependencies.Map));
        var cycles = Timed("domain cycles (IL)", () => DomainCycleScanner.Scan(dependencies.Edges));
        var holders = Timed("static state (IL)", () => StaticStateScanner.Scan(facts, graph));
        var written = Timed("static field writes", () => StaticFieldWriteScanner.Written(snapshot, holders.SelectMany(holder => holder.Fields).ToList()));

        var measures = new List<DebtMeasure>
        {
            DebtMeasures.StaticLookups(sites, snapshot),
            DebtMeasures.LargeTypes(types),
            DebtMeasures.LongMethods(members),
            DebtMeasures.ServicesPerType(serverSites, constructorServices, snapshot.Server, exemptions),
            DebtMeasures.DeferredResolution(constructorServices, providerUses),
            DebtMeasures.LogicToContent(content),
            DebtMeasures.DomainCycles(cycles),
            DebtMeasures.StaticState(holders, written, exemptions)
        };

        return new DebtScan(measures, timings);
    }
}
