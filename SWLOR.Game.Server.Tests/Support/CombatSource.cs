using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>Service.Combat</c> wherever it lives, for the tests that guard a rule by reading the code. While
/// <c>Combat</c> is split (<c>Readmes/MaintainabilityRefactorCombatSplit.md</c>) its text moves from
/// <c>Service/Combat.cs</c> to <c>Service/CombatParts/*.cs</c> and to the part services
/// <c>Domain/Combat/Combat*Service.cs</c> and <c>CombatRules.cs</c> (see <see cref="CombatParts"/>); a test calls
/// <see cref="Read"/> and keeps its needles.
/// <para>
/// The text is combined as it is, apart from two rewrites in the part services, which change a line only where the
/// move itself changed it: <c>static</c> is put back on a member declaration at member indentation, and a call on an
/// injected part (<c>_queries.GetFoo(x)</c>, a field declared as <c>ICombat...Service</c> in the same file, where
/// <c>Combat...Service</c> is a part by <see cref="CombatParts.IsPartTypeName"/>) loses its receiver (<c>GetFoo(x)</c>).
/// A call on another injected service, <c>ICombatRecorderService</c> included, keeps it. The one-line forwarders a moved
/// member leaves in the partial class are left out (<see cref="ElidePartForwarders"/>), so each member appears once,
/// with its body. Nothing else is rewritten, so a changed condition, a removed or reordered call, or a different argument
/// still fails a needle. <see cref="Normalise"/> is public so the tests of this helper can show that.
/// </para>
/// </summary>
public static class CombatSource
{
    // Members of the class sit at this indentation in the repository's block-namespace files.
    private const string MemberIndent = "        ";

    private static readonly Lazy<IReadOnlyList<SourceFile>> Loaded = new(() => Load(FindRepositoryRoot()));

    private static readonly Regex InjectedField = new(
        @"^\s*(?:private|protected|internal|public)\s+(?:readonly\s+)?I(?<type>Combat\w*Service)\s+(?<name>_\w+)\s*(?:;|=)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex InstanceMember = new(
        "^(?<head>" + MemberIndent + @"(?:public|private|internal|protected))(?<rest> (?!static\b|const\b|class\b|struct\b|enum\b|record\b|interface\b|delegate\b|event\b|override\b|virtual\b|abstract\b|sealed\b|partial\b|new\b|extern\b|unsafe\b|(?:readonly\s+)?I\w*Service\s+_).*)$",
        RegexOptions.CultureInvariant | RegexOptions.Singleline);

    /// <summary>One file of the combined source: its full path and its (normalised) text.</summary>
    public sealed record SourceFile(string FullPath, string Text);

    /// <summary>The combined source of <c>Combat</c>: <c>Combat.cs</c>, its parts, then the part services.</summary>
    public static string Read() => Combine(Loaded.Value);

    /// <summary>The full paths of the files <see cref="Read"/> combines, in the same order.</summary>
    public static IReadOnlyList<string> Paths() => Loaded.Value.Select(file => file.FullPath).ToArray();

    /// <summary>One syntax tree per file, for tests that look at members instead of text (file-scoped namespaces cannot be joined).</summary>
    public static IReadOnlyList<SyntaxTree> Trees() => Trees(Loaded.Value);

    /// <summary>Every node of a type across the files, for tests that find a method or a call by name.</summary>
    public static IEnumerable<TNode> Nodes<TNode>()
        where TNode : SyntaxNode =>
        Trees().SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TNode>());

    /// <summary>
    /// The method of this name that holds the code. While a moved body keeps a one-line forwarder of the same name in the
    /// partial class, the declaration with the longest text is the body.
    /// </summary>
    public static MethodDeclarationSyntax Method(string name) =>
        Nodes<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == name)
            .OrderByDescending(method => method.FullSpan.Length)
            .First();

    /// <summary>
    /// The text of every method with one of these names (all overloads), in the order the names are given, for tests that
    /// check a method body without depending on where the partial class files place it.
    /// </summary>
    public static string Members(params string[] names) =>
        string.Join('\n', names.SelectMany(name => Nodes<MethodDeclarationSyntax>().Where(method => method.Identifier.Text == name)).Select(method => method.ToString()));

    /// <summary>Whether a path is one of the files <see cref="Read"/> combines.</summary>
    public static bool IsCombatFile(string path) =>
        Paths().Contains(Path.GetFullPath(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>The files of <c>Combat</c> under a repository root, normalised, in reading order.</summary>
    public static IReadOnlyList<SourceFile> Load(string repositoryRoot)
    {
        var server = Path.Combine(repositoryRoot, "SWLOR.Game.Server");
        var files = new List<SourceFile>();

        var facade = Path.Combine(server, "Service", "Combat.cs");
        if (File.Exists(facade))
            files.Add(new SourceFile(Path.GetFullPath(facade), File.ReadAllText(facade)));

        foreach (var path in Sorted(Path.Combine(server, "Service", "CombatParts"), "*.cs", _ => true))
            files.Add(new SourceFile(Path.GetFullPath(path), ElidePartForwarders(File.ReadAllText(path))));

        foreach (var path in Sorted(Path.Combine(server, "Domain", "Combat"), "Combat*.cs", CombatParts.IsPartTypeName))
            files.Add(new SourceFile(
                Path.GetFullPath(path),
                Normalise(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path))));

        return files;
    }

    /// <summary>Joins files in order; each file ends with a line break so a needle never spans two files.</summary>
    public static string Combine(IEnumerable<SourceFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append(file.Text);
            if (!file.Text.EndsWith('\n'))
                builder.Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>One syntax tree per file.</summary>
    public static IReadOnlyList<SyntaxTree> Trees(IEnumerable<SourceFile> files) =>
        files.Select(file => CSharpSyntaxTree.ParseText(file.Text, path: file.FullPath)).ToArray();

    /// <summary>
    /// Puts a part service's text back into the form it had inside the static class: <c>static</c> on member
    /// declarations at member indentation (not constructors, constants, <c>virtual</c> and <c>override</c> members,
    /// nested types or the fields that hold injected parts), and no receiver on a call through a field declared as an
    /// <c>ICombat...Service</c> in the same file. Anything else stays as it is.
    /// </summary>
    public static string Normalise(string text, string typeName)
    {
        // Only a part of Combat lost its receiver in the move; a service of its own (ICombatRecorderService) kept its facade's.
        var injected = InjectedField.Matches(text)
            .Where(match => CombatParts.IsPartTypeName(match.Groups["type"].Value))
            .Select(match => match.Groups["name"].Value)
            .Distinct()
            .ToArray();
        var builder = new StringBuilder(text.Length);
        foreach (var line in SplitKeepingEndings(text))
        {
            var rewritten = RestoreStatic(line, typeName);
            foreach (var field in injected)
                rewritten = Regex.Replace(
                    rewritten,
                    $@"(?<![\w.]){Regex.Escape(field)}\.(?=[A-Za-z_]\w*\s*[<(])",
                    string.Empty);
            builder.Append(rewritten);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Drops, from a partial class file of <c>Combat</c>, the one-line forwarders to a part service: a method whose whole
    /// body is <c>=&gt; Part.SameName(...)</c>, where <c>Part</c> is a property of that file declared as the interface of a
    /// part (<c>private static ICombatQueriesService Queries =&gt; ...</c>). The member's body is in the part service, so the
    /// combined source lists it once, as the static class did. The forwarders themselves are proven by
    /// <c>CombatPartForwardingTests</c>; forwarders to <c>CombatRules</c> and everything else stay.
    /// </summary>
    public static string ElidePartForwarders(string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        var parts = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                               property.Type is IdentifierNameSyntax { Identifier.Text: var type } &&
                               type.Length > 1 && type[0] == 'I' && CombatParts.IsPartTypeName(type[1..]) &&
                               type[1..] != CombatParts.RulesTypeName)
            .Select(property => property.Identifier.Text)
            .ToHashSet(StringComparer.Ordinal);
        var forwarders = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.ExpressionBody?.Expression is InvocationExpressionSyntax
                             {
                                 Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } access
                             } &&
                             parts.Contains(receiver.Identifier.Text) &&
                             access.Name.Identifier.Text == method.Identifier.Text)
            .ToArray();

        return forwarders.Length == 0
            ? text
            : root.RemoveNodes(forwarders, SyntaxRemoveOptions.KeepNoTrivia)!.ToFullString();
    }

    private static string RestoreStatic(string line, string typeName)
    {
        var body = line.TrimEnd('\r', '\n');
        var match = InstanceMember.Match(body);
        if (!match.Success)
            return line;

        // A constructor repeats the type name: "public CombatFooService(".
        if (match.Groups["rest"].Value.TrimStart().StartsWith(typeName + "(", StringComparison.Ordinal))
            return line;

        return match.Groups["head"].Value + " static" + match.Groups["rest"].Value + line[body.Length..];
    }

    private static IEnumerable<string> SplitKeepingEndings(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var end = text.IndexOf('\n', start);
            end = end < 0 ? text.Length : end + 1;
            yield return text[start..end];
            start = end;
        }
    }

    private static IEnumerable<string> Sorted(string folder, string pattern, Func<string, bool> isPart) =>
        Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, pattern, SearchOption.TopDirectoryOnly)
                .Where(path => isPart(Path.GetFileNameWithoutExtension(path)))
                .OrderBy(path => path, StringComparer.Ordinal)
            : Enumerable.Empty<string>();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
