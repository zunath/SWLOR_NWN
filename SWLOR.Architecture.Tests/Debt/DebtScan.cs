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

    /// <summary>The ten measures, in the plan's order (P5-0's eight, then P5-1b's two).</summary>
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
        var references = Timed("references (bound in the server)", () => SourceReferenceScanner.Scan(snapshot));
        var cycles = Timed("domain cycles", () => DomainCycleScanner.Scan(references, dependencies.Map));
        var upward = Timed("upward references", () => DomainCycleScanner.Upward(references, dependencies.Map));
        var sinks = Timed("sink work", () => SinkWorkScanner.Scan(snapshot));
        var holders = Timed("static state (IL)", () => StaticStateScanner.Scan(facts, graph));
        var written = Timed("static field writes", () => StaticFieldWriteScanner.Written(snapshot, holders.SelectMany(holder => holder.Fields).ToList()));

        var measures = new List<DebtMeasure>
        {
            DebtMeasures.StaticLookups(sites, snapshot),
            DebtMeasures.LargeTypes(types),
            DebtMeasures.LongMethods(members),
            DebtMeasures.ServicesPerType(serverSites, constructorServices, snapshot.Server, exemptions),
            DebtMeasures.DeferredResolution(constructorServices, providerUses, dependencies.Map),
            DebtMeasures.LogicToContent(content),
            DebtMeasures.DomainCycles(cycles),
            DebtMeasures.StaticState(holders, written, exemptions),
            DebtMeasures.UpwardEdges(upward),
            DebtMeasures.SinkWork(sinks)
        };

        return new DebtScan(measures, timings);
    }
}
