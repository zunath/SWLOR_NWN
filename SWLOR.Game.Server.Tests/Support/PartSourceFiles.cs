using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The reading half of a source reader like <see cref="StatusEffectSource"/>: the files a service lives in (its own file first,
/// then each part the split moves out), read as they are apart from the receiver of a call through a field of a part interface,
/// which the code called on its own instance before the move. <see cref="EnmitySource"/> and <see cref="DeathSource"/> hold one each,
/// so a PR that moves a part adds its file to one list and no test names a path.
/// </summary>
public sealed class PartSourceFiles
{
    private readonly IReadOnlyList<string> _partFiles;
    private readonly IReadOnlySet<string> _partInterfaces;
    private readonly Lazy<IReadOnlyList<StatSource.SourceFile>> _loaded;

    public PartSourceFiles(IReadOnlyList<string> partFiles, IReadOnlySet<string> partInterfaces)
    {
        _partFiles = partFiles;
        _partInterfaces = partInterfaces;
        _loaded = new Lazy<IReadOnlyList<StatSource.SourceFile>>(() => Load(FindRepositoryRoot()));
    }

    /// <summary>The combined source: the part files, in reading order.</summary>
    public string Read() => StatSource.Combine(_loaded.Value);

    /// <summary>The full paths of the files <see cref="Read"/> combines, in the same order.</summary>
    public IReadOnlyList<string> Paths() => _loaded.Value.Select(file => file.FullPath).ToArray();

    /// <summary>One syntax tree per file.</summary>
    public IReadOnlyList<SyntaxTree> Trees() => StatSource.Trees(_loaded.Value);

    /// <summary>Every node of a type across the files.</summary>
    public IEnumerable<TNode> Nodes<TNode>()
        where TNode : SyntaxNode =>
        Trees().SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TNode>());

    /// <summary>The method of this name that holds the code: the declaration with the longest text, when a name has several overloads.</summary>
    public MethodDeclarationSyntax Method(string name) =>
        Nodes<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == name)
            .OrderByDescending(method => method.FullSpan.Length)
            .First();

    /// <summary>The method of this name, as the files hold it, found in whichever part file declares it.</summary>
    public MethodDeclarationSyntax SingleMethod(string name) =>
        Nodes<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);

    private IReadOnlyList<StatSource.SourceFile> Load(string repositoryRoot) =>
        _partFiles
            .Select(relative => Path.GetFullPath(Path.Combine(repositoryRoot, relative)))
            .Select(path => new StatSource.SourceFile(path, StatSource.Normalise(File.ReadAllText(path), _partInterfaces)))
            .ToArray();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
