using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using System.Text.Json;

namespace SWLOR.AnimationDrafts;

/// <summary>Resolves installed clips with the same child-before-parent precedence as model inheritance.</summary>
public sealed class InstalledMotionLibrary
{
    public static string? FindMountedRepository(string overlayPath)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(overlayPath))!;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        for (var directory = new DirectoryInfo(folder); directory != null; directory = directory.Parent)
        {
            var configPath = Path.Combine(directory.FullName, "Build", "hakbuilder.json");
            if (!File.Exists(configPath)) continue;
            using var reader = new StreamReader(new MemoryStream(AnimationSourceFile.ReadBytes(
                configPath, AnimationProject.MaximumFileBytes, "HAK configuration")));
            using var config = JsonDocument.Parse(reader.ReadToEnd());
            foreach (var layer in config.RootElement.GetProperty("HakList").EnumerateArray())
            {
                var layerPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(configPath)!, layer.GetProperty("Path").GetString()!)));
                // HAK source indexing is nonrecursive: a nested export folder is not mounted.
                if (folder.Equals(layerPath, comparison)) return directory.FullName;
            }
            return null;
        }
        return null;
    }

    public static byte[] ReadBank(string path, ref long loadedBytes)
    {
        if (loadedBytes < 0 || loadedBytes > AnimationInstall.MaximumInputBytes)
            throw new InvalidDataException("Invalid installed animation input budget.");
        var remaining = (int)(AnimationInstall.MaximumInputBytes - loadedBytes);
        var bytes = AnimationSourceFile.ReadBytes(path, Math.Min(AnimationProject.MaximumFileBytes, remaining),
            "Installed animation banks");
        loadedBytes += bytes.Length;
        return bytes;
    }

    private readonly MdlModel _overlay;
    private readonly Func<string, MdlModel?> _loadSuperModel;
    private readonly Dictionary<string, MdlModel> _models = new(StringComparer.OrdinalIgnoreCase);

    public InstalledMotionLibrary(MdlModel overlay, Func<string, MdlModel?> loadSuperModel)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _loadSuperModel = loadSuperModel ?? throw new ArgumentNullException(nameof(loadSuperModel));
        _models[overlay.Name] = overlay;
    }

    /// <summary>Decodes every cached parent against the budget already used for the overlay and body.</summary>
    public InstalledMotionLibrary(MdlModel overlay, Func<string, byte[]?> loadSuperModel, MdlReadBudget decodedBudget)
        : this(overlay, name => loadSuperModel(name) is { } bytes ? new MdlReader().Parse(bytes, decodedBudget) : null)
    {
        ArgumentNullException.ThrowIfNull(loadSuperModel);
        ArgumentNullException.ThrowIfNull(decodedBudget);
    }

    public (MdlModel Owner, MdlAnimation Animation) Resolve(string animationName)
    {
        AnimationProject.ValidateToken(animationName, 16);
        var current = _overlay;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current.Name };
        for (var depth = 0; depth < AnimationInstall.MaximumModelChainDepth; depth++)
        {
            var animation = current.Animations.SingleOrDefault(a =>
                a.Name.Equals(animationName, StringComparison.OrdinalIgnoreCase));
            if (animation != null) return (current, animation);

            var parent = current.SuperModel;
            if (string.IsNullOrWhiteSpace(parent) || parent.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Installed animation '{animationName}' was not found in the '{_overlay.Name}' model chain.");
            AnimationProject.ValidateToken(parent, 16);
            if (!visited.Add(parent))
                throw new InvalidDataException($"Cycle in the installed animation model chain at '{parent}'.");
            if (depth + 1 == AnimationInstall.MaximumModelChainDepth)
                throw new InvalidDataException("Installed animation model chain exceeds the supported depth.");
            if (!_models.TryGetValue(parent, out var loaded))
            {
                loaded = _loadSuperModel(parent) ?? throw new InvalidDataException($"Missing animation supermodel '{parent}' while resolving '{animationName}'.");
                _models.Add(parent, loaded);
            }
            current = loaded;
        }
        throw new InvalidDataException("Installed animation model chain exceeds the supported depth.");
    }
}
