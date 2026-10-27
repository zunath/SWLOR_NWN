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
    /// The direct reads of <c>DateTime.UtcNow</c> and <c>Environment.TickCount64</c> in the source, by line. Comments and
    /// string contents are ignored because the source is parsed, not searched.
    /// </summary>
    public static IReadOnlyList<(int Line, string Text)> FindDirectReads(string source)
    {
        var reads = new List<(int, string)>();
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var target = access.Expression.WithoutTrivia().ToString();
            var member = access.Name.Identifier.ValueText;
            var isDirectRead = (member == "UtcNow" && SystemClockTypes.Contains(target))
                               || (member == "TickCount64" && SystemEnvironmentTypes.Contains(target));
            if (isDirectRead)
                reads.Add((access.GetLocation().GetLineSpan().StartLinePosition.Line + 1, access.WithoutTrivia().ToString()));
        }

        return reads;
    }
}
