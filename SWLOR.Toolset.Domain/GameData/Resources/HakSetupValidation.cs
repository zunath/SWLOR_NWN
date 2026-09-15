using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources;

/// <summary>Checks asset availability before the workspace starts loading game data.</summary>
public static class HakSetupValidation
{
    public static void Validate(string repositoryRoot, IReadOnlyList<ResourceIndex.HakLayer>? packedLayers = null)
        => Validate(repositoryRoot, packedLayers,
            directory => Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly));

    internal static void Validate(string repositoryRoot, IReadOnlyList<ResourceIndex.HakLayer>? packedLayers,
        Func<string, IEnumerable<string>> enumerateFiles)
    {
        var root = Path.Combine(repositoryRoot, "SWLOR_Haks");
        var missing = new List<string>();

        // These source assets also back repository editors and service registration, even when
        // the renderer uses the module's installed archives.
        var tlk = Path.Combine(root, "sw_tlk", "sw_tlk.tlk.json");
        if (!HasNonemptyFile(tlk))
            missing.Add("sw_tlk/sw_tlk.tlk.json");
        if (!HasResources(Path.Combine(root, "sw_2da"), enumerateFiles, ".2da"))
            missing.Add("sw_2da (missing or empty)");

        if (packedLayers == null)
        {
            foreach (var layer in ResourceIndex.ReadHakLayers(
                         Path.Combine(repositoryRoot, "Build", "hakbuilder.json"), root))
            {
                if (!HasResources(layer.DirectoryPath, enumerateFiles) &&
                    !layer.Name.Equals("sw_2da", StringComparison.OrdinalIgnoreCase))
                    missing.Add($"{layer.Name} (missing or empty)");
            }
        }
        else
        {
            foreach (var layer in packedLayers)
                if (!HasArchiveResources(layer.DirectoryPath))
                    missing.Add(layer.DirectoryPath);
        }

        if (missing.Count == 0)
            return;

        var preview = string.Join(", ", missing.Take(8));
        if (missing.Count > 8)
            preview += $", and {missing.Count - 8} more";
        throw new InvalidOperationException(
            "HAK content is not ready. The toolset needs these assets to render areas.\n\n" +
            $"Missing, empty, or unreadable content: {preview}\n\n" +
            $"Open a terminal in:\n{repositoryRoot}\n\n" +
            "Run:\ngit submodule update --init --recursive -- SWLOR_Haks\n\n" +
            "If the HAK checkout is sparse, restore the omitted content:\n" +
            "git -C SWLOR_Haks sparse-checkout disable\n\n" +
            "Wait for the checkout to finish, then restart the toolset. " +
            "If you use installed HAKs, also check the HAK directory configured in nwn.ini.");
    }

    private static bool HasArchiveResources(string path)
    {
        try
        {
            return HakArchiveCatalog.Open(path).ResourceCount > 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            return false;
        }
    }

    private static bool HasNonemptyFile(string path)
    {
        try
        {
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool HasResources(string directory, Func<string, IEnumerable<string>> enumerateFiles,
        string? requiredExtension = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return false;

            var foundResource = false;
            foreach (var path in enumerateFiles(directory))
            {
                // Scan the whole directory even after finding content: a late enumeration failure
                // would also prevent HakDirectoryCatalog from loading this layer.
                if (foundResource)
                    continue;
                var extension = Path.GetExtension(path);
                var qualifies = requiredExtension == null
                    // BMU music is shipped beside the game's indexed Aurora resources.
                    ? ResourceIdentity.TypeFromExtension(extension) != ResourceTypes.Invalid ||
                      extension.Equals(".bmu", StringComparison.OrdinalIgnoreCase)
                    : extension.Equals(requiredExtension, StringComparison.OrdinalIgnoreCase);
                foundResource = qualifies && HasNonemptyFile(path);
            }
            return foundResource;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
