using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace SWLOR.TypeMoves;

/// <summary>
/// The metadata references of a project: the .NET runtime the tool runs on (the projects target the same framework), ASP.NET
/// Core for a web project, and the package assemblies the restore recorded in <c>obj/project.assets.json</c>. A project
/// without that file (not restored) compiles without its packages, so names from packages do not bind; the tool says so.
/// </summary>
internal sealed class ReferenceResolver
{
    private readonly Dictionary<string, MetadataReference> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<MetadataReference>? _framework;
    private IReadOnlyList<MetadataReference>? _aspNet;

    public IReadOnlyList<MetadataReference> For(ProjectInfo project, ICollection<string> warnings)
    {
        var references = new List<MetadataReference>(Framework);
        var assets = Path.Combine(project.Folder, "obj", "project.assets.json");
        var aspNet = false;
        var packages = File.Exists(assets) ? PackageAssemblies(assets, out aspNet) : null;
        if (packages == null)
        {
            aspNet = project.Web;
            warnings.Add($"{Path.GetFileName(project.ProjectFile)} has no obj/project.assets.json (not restored): its package types do not bind. Run dotnet restore for an exact result.");
        }

        if (aspNet)
            references.AddRange(AspNet);
        foreach (var path in packages ?? [])
            references.Add(Reference(path));

        return references;
    }

    private IReadOnlyList<MetadataReference> Framework => _framework ??= RuntimeAssemblies();

    private IReadOnlyList<MetadataReference> AspNet => _aspNet ??= SharedFramework("Microsoft.AspNetCore.App");

    private MetadataReference Reference(string path)
    {
        if (!_byPath.TryGetValue(path, out var reference))
        {
            reference = MetadataReference.CreateFromFile(path);
            _byPath[path] = reference;
        }

        return reference;
    }

    /// <summary>The assemblies of the runtime the tool runs on (the trusted platform assemblies in its runtime folder).</summary>
    private IReadOnlyList<MetadataReference> RuntimeAssemblies()
    {
        var runtime = Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory());
        var trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return trusted
            .Where(path => Path.GetFullPath(path).StartsWith(runtime, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Reference)
            .ToList();
    }

    /// <summary>A shared framework beside the runtime (<c>dotnet/shared/&lt;name&gt;/&lt;version&gt;</c>), the version nearest the runtime's.</summary>
    private IReadOnlyList<MetadataReference> SharedFramework(string name)
    {
        var runtime = new DirectoryInfo(Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory()).TrimEnd(Path.DirectorySeparatorChar));
        var shared = runtime.Parent?.Parent;
        var folder = shared == null ? null : new DirectoryInfo(Path.Combine(shared.FullName, name));
        if (folder is not { Exists: true })
            return [];

        var version = folder.GetDirectories().FirstOrDefault(directory => directory.Name == runtime.Name)
                      ?? folder.GetDirectories().OrderByDescending(directory => directory.Name, StringComparer.Ordinal).FirstOrDefault();
        if (version == null)
            return [];

        return version.GetFiles("*.dll").OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase).Select(file => Reference(file.FullName)).ToList();
    }

    /// <summary>The compile-time assemblies of every package of the project's first target in its assets file.</summary>
    private static List<string>? PackageAssemblies(string assetsPath, out bool aspNet)
    {
        aspNet = false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
            var root = document.RootElement;
            var folders = root.TryGetProperty("packageFolders", out var packageFolders)
                ? packageFolders.EnumerateObject().Select(folder => folder.Name).ToList()
                : [];
            var libraries = root.GetProperty("libraries");
            var target = root.GetProperty("targets").EnumerateObject().FirstOrDefault();
            if (target.Value.ValueKind != JsonValueKind.Object)
                return [];

            if (root.TryGetProperty("project", out var project) && project.TryGetProperty("frameworks", out var frameworks))
            {
                foreach (var framework in frameworks.EnumerateObject())
                {
                    if (framework.Value.TryGetProperty("frameworkReferences", out var frameworkReferences) &&
                        frameworkReferences.EnumerateObject().Any(reference => reference.Name == "Microsoft.AspNetCore.App"))
                        aspNet = true;
                }
            }

            var result = new List<string>();
            foreach (var library in target.Value.EnumerateObject())
            {
                if (!library.Value.TryGetProperty("type", out var type) || type.GetString() != "package" ||
                    !library.Value.TryGetProperty("compile", out var compile) || !libraries.TryGetProperty(library.Name, out var info) ||
                    !info.TryGetProperty("path", out var relative))
                    continue;

                foreach (var item in compile.EnumerateObject())
                {
                    if (item.Name.EndsWith("_._", StringComparison.Ordinal))
                        continue;

                    var path = folders
                        .Select(folder => Path.GetFullPath(Path.Combine(folder, relative.GetString()!, item.Name)))
                        .FirstOrDefault(File.Exists);
                    if (path != null)
                        result.Add(path);
                }
            }

            return result;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
