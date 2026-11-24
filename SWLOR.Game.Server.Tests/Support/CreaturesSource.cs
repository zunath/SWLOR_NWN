using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of a member of the three Creatures services (<c>AIService</c>, <c>CompanionControlService</c> and
/// <c>SpawnService</c>) wherever it lives, for the tests that guard a rule by reading the code. The Creatures split
/// (<c>Readmes/MaintainabilityRefactorCreaturesSplit.md</c>) moves members into part files; each pull request that moves a
/// part adds its file to the <see cref="Groups"/> entry of the service it leaves, and the interface of the part to that entry's
/// interfaces. A test asks for a member by name (<see cref="Member"/>, <see cref="Members"/>) or for the file that declares it
/// (<see cref="RelativePath"/>) and keeps its needles.
/// <para>
/// As in <see cref="StatSource"/>, the text is as it is apart from one rewrite: a call through a field of a part interface
/// of the same original service (<c>_authority.PeekAuthorizedTarget(x)</c>) loses its receiver, because the code called that
/// member on its own instance before the move. A call on any other injected service keeps its receiver.
/// </para>
/// </summary>
public static class CreaturesSource
{
    /// <summary>The files and part interfaces of one original service, in reading order.</summary>
    public sealed record Group(string Service, IReadOnlyList<string> Files, IReadOnlySet<string> Interfaces);

    /// <summary>
    /// One group per original service. <c>Files</c> are repository-relative; the service itself comes first, then each part in
    /// the order the split moves it.
    /// </summary>
    public static readonly IReadOnlyList<Group> Groups = new[]
    {
        new Group(
            "AIService",
            new[]
            {
                "SWLOR.Game.Server/Domain/Creatures/AIService.cs",
                "SWLOR.Game.Server/Domain/Creatures/AICreatureEventService.cs",
                "SWLOR.Game.Server/Domain/Creatures/AIAggroService.cs",
                "SWLOR.Game.Server/Domain/Creatures/AIDecisionService.cs",
                "SWLOR.Game.Server/Domain/Creatures/AIActionService.cs",
            },
            new HashSet<string>(StringComparer.Ordinal)
            {
                "IAIService",
                "IAICreatureEventService",
                "IAIAggroService",
                "IAIDecisionService",
                "IAIActionService",
            }),
        new Group(
            "CompanionControlService",
            new[]
            {
                "SWLOR.Game.Server/Domain/Creatures/CompanionControlService.cs",
                "SWLOR.Game.Server/Domain/Creatures/CompanionControlStoreService.cs",
                "SWLOR.Game.Server/Domain/Creatures/CompanionPositioningService.cs",
                "SWLOR.Game.Server/Domain/Creatures/CompanionTargetAuthorityService.cs",
            },
            new HashSet<string>(StringComparer.Ordinal)
            {
                "ICompanionControlService",
                "ICompanionControlStoreService",
                "ICompanionPositioningService",
                "ICompanionTargetAuthorityService",
            }),
        new Group(
            "SpawnService",
            new[] { "SWLOR.Game.Server/Domain/Creatures/SpawnService.cs" },
            new HashSet<string>(StringComparer.Ordinal) { "ISpawnService" }),
    };

    private const string ServerPrefix = "SWLOR.Game.Server/";

    private static readonly Lazy<IReadOnlyList<(string Relative, SyntaxTree Tree)>> Loaded = new(Load);

    /// <summary>Every file of every group, repository-relative, in reading order.</summary>
    public static IReadOnlyList<string> PartFiles => Groups.SelectMany(group => group.Files).ToArray();

    /// <summary>One syntax tree per file (the text normalised as described above).</summary>
    public static IReadOnlyList<SyntaxTree> Trees() => Loaded.Value.Select(file => file.Tree).ToArray();

    /// <summary>The text of the method of this name (the longest, when it has overloads), attributes included.</summary>
    public static string Member(string name) => Declarations(name)
        .OrderByDescending(entry => entry.Method.FullSpan.Length)
        .First()
        .Method
        .ToString();

    /// <summary>The text of every method with one of these names (all overloads), in the order the names are given.</summary>
    public static string Members(params string[] names) =>
        string.Join('\n', names.SelectMany(name => Declarations(name)).Select(entry => entry.Method.ToString()));

    /// <summary>
    /// The path, relative to the server project (<c>Domain/Creatures/AIService.cs</c>), of the file that declares the method of
    /// this name, for the harnesses that borrow a method by path.
    /// </summary>
    public static string RelativePath(string name) => Declarations(name)
        .OrderByDescending(entry => entry.Method.FullSpan.Length)
        .First()
        .Relative;

    private static IEnumerable<(string Relative, MethodDeclarationSyntax Method)> Declarations(string name)
    {
        var found = Loaded.Value
            .SelectMany(file => file.Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.Text == name)
                .Select(method => (file.Relative, method)))
            .ToArray();
        if (found.Length == 0)
            throw new InvalidOperationException($"No Creatures file declares a method named {name}.");
        return found;
    }

    private static IReadOnlyList<(string Relative, SyntaxTree Tree)> Load()
    {
        var root = FindRepositoryRoot();
        return Groups
            .SelectMany(group => group.Files.Select(relative => (Group: group, Relative: relative)))
            .Select(entry =>
            {
                var path = Path.GetFullPath(Path.Combine(root, entry.Relative));
                var text = StatSource.Normalise(File.ReadAllText(path), entry.Group.Interfaces);
                return (entry.Relative[ServerPrefix.Length..], CSharpSyntaxTree.ParseText(text, path: path));
            })
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
