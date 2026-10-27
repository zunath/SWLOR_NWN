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
    // What the game project's implicit usings import, so a listed file binds the same way it does in the build.
    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray());

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
    /// The direct reads of <c>DateTime.UtcNow</c> and <c>Environment.TickCount64</c> in the source, by line. The source is
    /// bound against the framework, so aliases (<c>using UtcClock = System.DateTime;</c>, <c>using Sys = global::System;</c>),
    /// <c>using static</c> and any spelling of the type are all resolved; comments and string contents are ignored.
    /// </summary>
    public static IReadOnlyList<(int Line, string Text)> FindDirectReads(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "ClockSeamGuard",
            new[] { tree, CSharpSyntaxTree.ParseText(ImplicitUsings) },
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);

        var reads = new List<(int, string)>();
        foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
        {
            var member = name.Identifier.ValueText;
            if (member != "UtcNow" && member != "TickCount64")
                continue;

            if (model.GetSymbolInfo(name).Symbol is not IPropertySymbol property)
                continue;

            var owner = property.ContainingType?.ToDisplayString();
            if ((member == "UtcNow" && owner == "System.DateTime") || (member == "TickCount64" && owner == "System.Environment"))
            {
                SyntaxNode expression = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
                reads.Add((expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1, expression.WithoutTrivia().ToString()));
            }
        }

        return reads.OrderBy(read => read.Item1).ToList();
    }
}
