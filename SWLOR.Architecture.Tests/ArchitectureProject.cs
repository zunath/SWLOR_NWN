using SWLOR.Architecture.Tests.Debt;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>Where the guard's inputs live, and the single scan of the server assembly all guard tests share.</summary>
internal static class ArchitectureProject
{
    /// <summary>Set to <c>1</c> to rewrite the baseline files instead of comparing with them (same switch as the other snapshot tests).</summary>
    public const string UpdateVariable = "SWLOR_UPDATE_SNAPSHOTS";

    private const string ProjectFileName = "SWLOR.Architecture.Tests.csproj";
    private const string ServerAssemblyFileName = "SWLOR.Game.Server.dll";
    private const string ContractsAssemblyFileName = "SWLOR.Game.Contracts.dll";
    private const string HooksAssemblyFileName = "SWLOR.Game.NativeHooks.dll";
    private const string ContractsNamespace = "SWLOR.Game.Contracts";

    private static readonly Lazy<TypeReferenceGraph> ServerGraph = new(ScanServerGraph);
    private static readonly Lazy<DomainDependencies> ServerDependencies = new(() => DomainDependencies.Build(ServerGraph.Value, DomainMap.Load(DomainMapPath)));
    private static readonly Lazy<AssemblyFacts> ServerAssemblyFacts = new(() => AssemblyFactScanner.Scan(ServerAssemblyPath));
    private static readonly Lazy<NativeHookFacts> ServerNativeHooks = new(() => NativeHookScanner.Scan(ServerAssemblyPath, ContractsNamespace));
    private static readonly Lazy<NativeHookFacts> HooksAssemblyNativeHooks = new(() => NativeHookScanner.Scan(HooksAssemblyPath, ContractsNamespace));
    private static readonly Lazy<SourceSnapshot> SolutionSource = new(() => SourceSnapshot.Load(RepositoryRoot, SolutionFileName));
    private static readonly Lazy<DebtScan> SolutionDebt = new(() => DebtScan.Run(Source, Exemptions, Graph, Server, Facts));

    /// <summary>The solution whose projects the Phase 5 debt measures read.</summary>
    public const string SolutionFileName = "SWLOR.Game.Server.sln";

    public static bool IsUpdating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    /// <summary>The project's source folder, found by walking up from the test binaries.</summary>
    public static string Directory
    {
        get
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, ProjectFileName)))
                    return folder.FullName;
            }

            throw new InvalidOperationException($"Could not find {ProjectFileName} above {AppContext.BaseDirectory}.");
        }
    }

    /// <summary>The repository root: the folder of the solution, one above this project.</summary>
    public static string RepositoryRoot => Path.GetFullPath(Path.Combine(Directory, ".."));

    /// <summary>The folder of the Phase 5 debt baselines, their exemptions and the generated report.</summary>
    public static string DebtBaselineDirectory => Path.Combine(Directory, "Baseline", "Phase5");

    /// <summary>The reviewed exemptions and approved static entry points of the Phase 5 debt measures.</summary>
    public static ExemptionList Exemptions => ExemptionList.Load(Path.Combine(DebtBaselineDirectory, "Exemptions.json"));

    /// <summary>Every project of the solution, parsed once for all debt tests.</summary>
    public static SourceSnapshot Source => SolutionSource.Value;

    /// <summary>The Phase 5 debt measures of the solution as it is now, computed once for all debt tests.</summary>
    public static DebtScan Debt => SolutionDebt.Value;

    public static string DomainMapPath => Path.Combine(Directory, "DomainMap.json");

    public static string ConvertedServicesPath => Path.Combine(Directory, "ConvertedServices.json");

    public static string EnforcedBaselinePath => Path.Combine(Directory, "Baseline", "CrossDomainEdges.txt");

    public static string UnrestrictedBaselinePath => Path.Combine(Directory, "Baseline", "EdgesIntoCoreAndPlatform.txt");

    /// <summary>The edges between the server's domains as they are in the compiled assembly right now.</summary>
    public static DomainDependencies Server => ServerDependencies.Value;

    /// <summary>Which type of the compiled server assembly references which other type.</summary>
    public static TypeReferenceGraph Graph => ServerGraph.Value;

    /// <summary>The types, static fields, event handlers and uses of other assemblies' types of the compiled server assembly.</summary>
    public static AssemblyFacts Facts => ServerAssemblyFacts.Value;

    /// <summary>The services converted to the dependency-injection pattern, from <c>ConvertedServices.json</c>.</summary>
    public static ConvertedServiceList ConvertedServices => ConvertedServiceList.Load(ConvertedServicesPath);

    /// <summary>The native hook entry points, the sink implementations and the sink interfaces of the compiled server assembly.</summary>
    public static NativeHookFacts NativeHooks => ServerNativeHooks.Value;

    /// <summary>The trampolines, the sink implementations and the sink interfaces of the compiled native hooks assembly.</summary>
    public static NativeHookFacts HooksAssemblyHooks => HooksAssemblyNativeHooks.Value;

    /// <summary>The compiled native hooks assembly (<c>SWLOR.Game.NativeHooks</c>), copied next to the tests.</summary>
    public static string HooksAssemblyPath => Path.Combine(AppContext.BaseDirectory, HooksAssemblyFileName);

    /// <summary>The compiled contracts assembly (<c>SWLOR.Game.Contracts</c>), copied next to the tests.</summary>
    public static string ContractsAssemblyPath => Path.Combine(AppContext.BaseDirectory, ContractsAssemblyFileName);

    /// <summary>The compiled permanent host (<c>SWLOR.Game.Host</c>), copied next to the tests.</summary>
    public static string HostAssemblyPath => Path.Combine(AppContext.BaseDirectory, "SWLOR.Game.Host.dll");

    private static string ServerAssemblyPath =>Path.Combine(AppContext.BaseDirectory, ServerAssemblyFileName);

    private static TypeReferenceGraph ScanServerGraph() => AssemblyScanner.Scan(ServerAssemblyPath);
}
