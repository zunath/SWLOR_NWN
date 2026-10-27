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

    private static readonly Lazy<DomainDependencies> ServerDependencies = new(ScanServer);

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

    public static string EnforcedBaselinePath => Path.Combine(Directory, "Baseline", "CrossDomainEdges.txt");

    public static string UnrestrictedBaselinePath => Path.Combine(Directory, "Baseline", "EdgesIntoCoreAndPlatform.txt");

    /// <summary>The edges between the server's domains as they are in the compiled assembly right now.</summary>
    public static DomainDependencies Server => ServerDependencies.Value;

    private static DomainDependencies ScanServer()
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, ServerAssemblyFileName);
        var graph = AssemblyScanner.Scan(assemblyPath);
        return DomainDependencies.Build(graph, DomainMap.Load(DomainMapPath));
    }
}
