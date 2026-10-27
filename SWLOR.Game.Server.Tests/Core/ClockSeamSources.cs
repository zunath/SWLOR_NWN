using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Reads <c>tools/Refactor/ClockSeamFiles.txt</c> (the files <c>Apply-ClockSeam.ps1</c> owns) and finds direct reads of
/// the system clock in C# source.
/// </summary>
public static class ClockSeamSources
{
    private static readonly string[] SystemClockTypes = { "DateTime", "System.DateTime", "global::System.DateTime" };
    private static readonly string[] SystemEnvironmentTypes = { "Environment", "System.Environment", "global::System.Environment" };

    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        Assert.That(directory, Is.Not.Null, "SWLOR.Game.Server.sln was not found above the test directory.");
        return directory!.FullName;
    }

    public static string ManifestPath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "tools", "Refactor", "ClockSeamFiles.txt");

    /// <summary>The manifest entries, as written (relative to <c>SWLOR.Game.Server/</c>).</summary>
    public static IReadOnlyList<string> ReadEntries(string repositoryRoot) =>
        File.ReadAllLines(ManifestPath(repositoryRoot))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();

    /// <summary>Every file the manifest lists, expanding <c>folder/**</c> to the .cs files under it.</summary>
    public static IReadOnlyList<string> ListFiles(string repositoryRoot)
    {
        var serverRoot = Path.Combine(repositoryRoot, "SWLOR.Game.Server");
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in ReadEntries(repositoryRoot))
        {
            if (entry.EndsWith("/**", StringComparison.Ordinal))
            {
                var folder = Path.Combine(serverRoot, entry[..^3]);
                Directory.Exists(folder).Should().BeTrue($"manifest folder {entry} must exist");
                foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
                    files.Add(file);
            }
            else
            {
                var file = Path.Combine(serverRoot, entry);
                File.Exists(file).Should().BeTrue($"manifest file {entry} must exist");
                files.Add(file);
            }
        }

        return files.ToList();
    }

    /// <summary>
    /// The direct reads of <c>DateTime.UtcNow</c> and <c>Environment.TickCount64</c> in the source, by line, including
    /// through a using alias or a <c>using static</c> of those types. Comments and string contents are ignored because
    /// the source is parsed, not searched.
    /// </summary>
    public static IReadOnlyList<(int Line, string Text)> FindDirectReads(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var clockTypes = new HashSet<string>(SystemClockTypes);
        var environmentTypes = new HashSet<string>(SystemEnvironmentTypes);
        var clockMemberIsImported = false;
        var environmentMemberIsImported = false;
        foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var target = directive.NamespaceOrType.WithoutTrivia().ToString();
            if (directive.Alias != null)
            {
                var alias = directive.Alias.Name.Identifier.ValueText;
                if (SystemClockTypes.Contains(target))
                    clockTypes.Add(alias);
                if (SystemEnvironmentTypes.Contains(target))
                    environmentTypes.Add(alias);
            }
            else if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            {
                clockMemberIsImported |= SystemClockTypes.Contains(target);
                environmentMemberIsImported |= SystemEnvironmentTypes.Contains(target);
            }
        }

        var reads = new List<(int, string)>();
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var target = access.Expression.WithoutTrivia().ToString();
            var member = access.Name.Identifier.ValueText;
            if ((member == "UtcNow" && clockTypes.Contains(target)) || (member == "TickCount64" && environmentTypes.Contains(target)))
                reads.Add((LineOf(access), access.WithoutTrivia().ToString()));
        }

        // A bare UtcNow / TickCount64 resolves to the system member only when its type was imported with using static.
        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var name = identifier.Identifier.ValueText;
            var isImportedMember = (name == "UtcNow" && clockMemberIsImported) || (name == "TickCount64" && environmentMemberIsImported);
            var isQualifiedOrNamed = identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                                     || identifier.Parent is MemberBindingExpressionSyntax
                                     || identifier.Parent is QualifiedNameSyntax
                                     || identifier.Parent is NameEqualsSyntax;
            if (isImportedMember && !isQualifiedOrNamed)
                reads.Add((LineOf(identifier), name));
        }

        return reads.OrderBy(read => read.Item1).ToList();
    }

    private static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
