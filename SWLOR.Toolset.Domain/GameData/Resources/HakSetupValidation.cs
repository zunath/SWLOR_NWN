using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources;

/// <summary>Checks asset availability before the workspace starts loading game data.</summary>
public static class HakSetupValidation
{
    public static void Validate(string repositoryRoot, IReadOnlyList<ResourceIndex.HakLayer>? packedLayers = null)
    {
        var root = Path.Combine(repositoryRoot, "SWLOR_Haks");
        var missing = new List<string>();

        // These source assets also back repository editors and service registration, even when
        // the renderer uses the module's installed archives.
        var tlk = Path.Combine(root, "sw_tlk", "sw_tlk.tlk.json");
        if (!File.Exists(tlk) || new FileInfo(tlk).Length == 0)
            missing.Add("sw_tlk/sw_tlk.tlk.json");
        if (!HasResources(Path.Combine(root, "sw_2da"), ".2da"))
            missing.Add("sw_2da (missing or empty)");

        if (packedLayers == null)
        {
            foreach (var layer in ResourceIndex.ReadHakLayers(
                         Path.Combine(repositoryRoot, "Build", "hakbuilder.json"), root))
            {
                if (!HasResources(layer.DirectoryPath) &&
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

    private static bool HasResources(string directory, string? requiredExtension = null) =>
        Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Any(path => (requiredExtension == null
                             // BMU music is shipped beside the game's indexed Aurora resources.
                             ? ResourceIdentity.TypeFromExtension(Path.GetExtension(path)) != ResourceTypes.Invalid ||
                               Path.GetExtension(path).Equals(".bmu", StringComparison.OrdinalIgnoreCase)
                             : Path.GetExtension(path).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase)) &&
                         new FileInfo(path).Length > 0);
}
