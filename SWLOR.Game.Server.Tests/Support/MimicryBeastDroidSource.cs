using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of a member of <c>MimicryService</c>, <c>BeastMasteryService</c> or <c>DroidService</c> wherever it lives, for the
/// tests that guard a rule by reading the code. The Character, Abilities and Companions split
/// (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>) moves members of the three services into part files; each pull
/// request that moves a part adds its file to the <see cref="Files"/> entry of the service it leaves, and the interface of the part to
/// that entry's interfaces. A test names a service and a member and keeps its needles; no test names a path.
/// <para>
/// The Perk, Skill and PlayerName services are read through their own reader. It holds one list per service, like
/// <see cref="EnmitySource"/>, so the two readers never share a line.
/// </para>
/// </summary>
public static class MimicryBeastDroidSource
{
    public const string Mimicry = "MimicryService";
    public const string Beast = "BeastMasteryService";
    public const string Droid = "DroidService";

    private const string ServerPrefix = "SWLOR.Game.Server/";

    /// <summary>The files of each service, repository-relative, in reading order: the service itself, then each part the split moves out.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Files = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        [Mimicry] = new[]
        {
            "SWLOR.Game.Server/Domain/Abilities/MimicryService.cs",
            "SWLOR.Game.Server/Domain/Abilities/MimicryLoadoutService.cs",
        },
        [Beast] = new[]
        {
            "SWLOR.Game.Server/Domain/Companions/BeastMasteryService.cs",
            "SWLOR.Game.Server/Domain/Companions/BeastSpawnService.cs",
            "SWLOR.Game.Server/Domain/Companions/BeastEventService.cs",
            "SWLOR.Game.Server/Domain/Companions/BeastIncubationService.cs",
        },
        [Droid] = new[] { "SWLOR.Game.Server/Domain/Companions/DroidService.cs" },
    };

    /// <summary>The interfaces of each service and of its parts, whose fields lose their receiver in the text (see <see cref="PartSourceFiles"/>).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Interfaces = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
    {
        [Mimicry] = new HashSet<string>(StringComparer.Ordinal) { "IMimicryService", "IMimicryLoadoutService" },
        [Beast] = new HashSet<string>(StringComparer.Ordinal) { "IBeastMasteryService", "IBeastSpawnService", "IBeastEventService", "IBeastIncubationService" },
        [Droid] = new HashSet<string>(StringComparer.Ordinal) { "IDroidService" },
    };

    private static readonly Dictionary<string, PartSourceFiles> Sources = Files.ToDictionary(
        entry => entry.Key,
        entry => new PartSourceFiles(entry.Value, Interfaces[entry.Key]),
        StringComparer.Ordinal);

    /// <summary>The combined source of the service.</summary>
    public static string Read(string service) => Sources[service].Read();

    /// <summary>The full paths of the files <see cref="Read"/> combines.</summary>
    public static IReadOnlyList<string> Paths(string service) => Sources[service].Paths();

    /// <summary>The full paths of the files of all three services.</summary>
    public static IReadOnlyList<string> AllPaths() => Files.Keys.SelectMany(Paths).ToArray();

    /// <summary>The method of this name that holds the code (the declaration with the longest text, when a name has several overloads).</summary>
    public static MethodDeclarationSyntax Method(string service, string name) => Sources[service].Method(name);

    /// <summary>The text of <see cref="Method"/>, attributes included.</summary>
    public static string Member(string service, string name) => Method(service, name).ToString();

    /// <summary>The method of this name as a static method of a stand-in class, for a harness that compiles production bodies.</summary>
    public static string InstanceMethod(string service, string name) =>
        Service.DMPossessionHarness.InstanceMethod(Sources[service].Trees().Select(tree => tree.GetRoot()), name);

    /// <summary>The path, relative to the server project (<c>Domain/Abilities/MimicryService.cs</c>), of the file that declares the method of this name.</summary>
    public static string RelativePath(string service, string name)
    {
        var tree = Sources[service].Trees().Single(candidate => candidate.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == name));
        var path = tree.FilePath.Replace('\\', '/');
        return path[(path.IndexOf(ServerPrefix, StringComparison.Ordinal) + ServerPrefix.Length)..];
    }
}
