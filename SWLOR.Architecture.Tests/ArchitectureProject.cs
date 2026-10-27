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

    private static readonly Lazy<TypeReferenceGraph> ServerGraph = new(ScanServerGraph);
    private static readonly Lazy<DomainDependencies> ServerDependencies = new(() => DomainDependencies.Build(ServerGraph.Value, DomainMap.Load(DomainMapPath)));
    private static readonly Lazy<AssemblyFacts> ServerAssemblyFacts = new(() => AssemblyFactScanner.Scan(ServerAssemblyPath));

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

    private static string ServerAssemblyPath => Path.Combine(AppContext.BaseDirectory, ServerAssemblyFileName);

    private static TypeReferenceGraph ScanServerGraph() => AssemblyScanner.Scan(ServerAssemblyPath);
}
