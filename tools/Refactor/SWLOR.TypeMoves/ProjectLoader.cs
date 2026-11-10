using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SWLOR.TypeMoves;

/// <summary>
/// Reads an SDK-style project file the way MSBuild would for compiling: the <c>.cs</c> files below its folder (without build
/// output, hidden folders and nested projects), <c>Compile</c> items, project references inside the repository, the
/// language version, the output kind, the global usings and <c>InternalsVisibleTo</c>. Conditions are ignored; the projects of
/// this repository use none that matter here.
/// </summary>
internal static class ProjectLoader
{
    private static readonly string[] DefaultUsings =
    [
        "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http", "System.Threading", "System.Threading.Tasks"
    ];

    private static readonly string[] WebUsings =
    [
        "System.Net.Http.Json", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Http",
        "Microsoft.AspNetCore.Routing", "Microsoft.Extensions.Configuration", "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting", "Microsoft.Extensions.Logging"
    ];

    /// <summary>Folders a project never compiles from.</summary>
    public static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "TestResults"
    };

    public static ProjectInfo Load(string projectFile)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(projectFile);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or IOException)
        {
            throw new TypeMoveException($"{projectFile} could not be read: {exception.Message}");
        }

        var root = document.Root!;
        var folder = Path.GetDirectoryName(projectFile)!;
        var sdk = (string?)root.Attribute("Sdk") ?? string.Empty;
        var web = sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);

        var assemblyName = Property(root, "AssemblyName") ?? Path.GetFileNameWithoutExtension(projectFile);
        var outputKind = (Property(root, "OutputType") ?? "Library").ToLowerInvariant() switch
        {
            "exe" => OutputKind.ConsoleApplication,
            "winexe" => OutputKind.WindowsApplication,
            _ => OutputKind.DynamicallyLinkedLibrary
        };

        var version = LanguageVersion.Preview;
        if (Property(root, "LangVersion") is { } langVersion && LanguageVersionFacts.TryParse(langVersion, out var parsed))
            version = parsed;

        var references = new List<string>();
        foreach (var item in Items(root, "ProjectReference"))
        {
            var include = (string?)item.Attribute("Include");
            if (include == null || include.Contains("$(", StringComparison.Ordinal))
                continue; // a path built from an MSBuild property points outside this tree

            var referenced = Path.GetFullPath(Path.Combine(folder, include.Replace('\\', Path.DirectorySeparatorChar)));
            if (File.Exists(referenced))
                references.Add(referenced);
        }

        var sources = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in DefaultSources(folder))
            sources.Add(file);
        foreach (var item in Items(root, "Compile"))
        {
            if ((string?)item.Attribute("Include") is { } include)
            {
                foreach (var file in Expand(folder, include))
                    sources.Add(file);
            }

            if ((string?)item.Attribute("Remove") is { } remove)
            {
                foreach (var file in sources.Where(file => PathPattern.Matches(remove.Replace('\\', '/'), Path.GetRelativePath(folder, file).Replace('\\', '/'))).ToList())
                    sources.Remove(file);
            }
        }

        var generated = new StringBuilder();
        var globalUsings = GeneratedGlobalUsings(folder, assemblyName);
        if (globalUsings != null)
        {
            generated.AppendLine(globalUsings);
        }
        else
        {
            var implicitUsings = Property(root, "ImplicitUsings");
            if (string.Equals(implicitUsings, "enable", StringComparison.OrdinalIgnoreCase) || string.Equals(implicitUsings, "true", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in web ? DefaultUsings.Concat(WebUsings) : DefaultUsings)
                    generated.AppendLine($"global using global::{name};");
            }

            foreach (var item in Items(root, "Using"))
            {
                var include = (string?)item.Attribute("Include");
                if (include == null)
                    continue;

                var alias = (string?)item.Attribute("Alias");
                var isStatic = string.Equals((string?)item.Attribute("Static"), "true", StringComparison.OrdinalIgnoreCase);
                generated.AppendLine(alias != null
                    ? $"global using {alias} = global::{include};"
                    : isStatic ? $"global using static global::{include};" : $"global using global::{include};");
            }
        }

        foreach (var item in Items(root, "InternalsVisibleTo"))
        {
            if ((string?)item.Attribute("Include") is { } friend)
                generated.AppendLine($"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{friend}\")]");
        }

        return new ProjectInfo(projectFile, assemblyName, outputKind, version, references, sources.ToList(), generated.ToString(), web);
    }

    /// <summary>Every <c>.cs</c> file below the folder, without build output, hidden folders and nested projects.</summary>
    public static IEnumerable<string> DefaultSources(string folder)
    {
        var pending = new Stack<string>();
        pending.Push(folder);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(current, "*.cs"))
                yield return Path.GetFullPath(file);

            foreach (var child in Directory.EnumerateDirectories(current))
            {
                var info = new DirectoryInfo(child);
                if (SkippedFolders.Contains(info.Name) || info.Name.StartsWith('.') || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                if (Directory.EnumerateFiles(child, "*.csproj").Any())
                    continue; // a nested project compiles its own files

                pending.Push(child);
            }
        }
    }

    /// <summary>The global usings the build generated for the project, from the newest build under <c>obj/</c>, or null.</summary>
    private static string? GeneratedGlobalUsings(string folder, string assemblyName)
    {
        var obj = Path.Combine(folder, "obj");
        if (!Directory.Exists(obj))
            return null;

        var newest = Directory.EnumerateFiles(obj, assemblyName + ".GlobalUsings.g.cs", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .FirstOrDefault();
        return newest == null ? null : File.ReadAllText(newest.FullName);
    }

    private static IEnumerable<string> Expand(string folder, string include)
    {
        var normalized = include.Replace('\\', '/');
        if (!normalized.Contains('*'))
        {
            var path = Path.GetFullPath(Path.Combine(folder, normalized));
            return File.Exists(path) ? [path] : [];
        }

        var star = normalized.IndexOf('*');
        var slash = normalized.LastIndexOf('/', star);
        var baseFolder = Path.GetFullPath(Path.Combine(folder, slash < 0 ? "." : normalized[..slash]));
        if (!Directory.Exists(baseFolder))
            return [];

        var pattern = slash < 0 ? normalized : normalized[(slash + 1)..];
        return Directory.EnumerateFiles(baseFolder, "*.cs", SearchOption.AllDirectories)
            .Where(file => PathPattern.Matches(pattern, Path.GetRelativePath(baseFolder, file).Replace('\\', '/')))
            .Select(Path.GetFullPath)
            .ToList();
    }

    private static string? Property(XElement root, string name) =>
        root.Descendants().FirstOrDefault(element => element.Name.LocalName == name && element.Parent?.Name.LocalName == "PropertyGroup")?.Value.Trim();

    private static IEnumerable<XElement> Items(XElement root, string name) =>
        root.Descendants().Where(element => element.Name.LocalName == name && element.Parent?.Name.LocalName == "ItemGroup");
}
