using SWLOR.Toolset.Domain.Distribution;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Distribution;

public static class ToolsetAssetPaths
{
    public static string? AssetRoot(string? moduleRoot)
    {
        var root = WorkspaceManifest.FindRoot(moduleRoot);
        if (root == null) return null;
        var manifest = WorkspaceManifest.Load(root);
        var assets = Path.Combine(WorkspaceDownload.AssetCacheDirectory, manifest.AssetRevision);
        AssetManifest.Load(assets, manifest.AssetRevision, verifyFiles: false);
        return assets;
    }

    public static NwnIniProfile Profile(string? moduleRoot, string? iniOverride = null)
    {
        var profile = NwnIniProfile.Load(iniOverride);
        var assets = AssetRoot(moduleRoot);
        return assets == null ? profile : profile with
        {
            HakDirectory = Path.Combine(assets, "hak"),
            TlkDirectory = Path.Combine(assets, "tlk")
        };
    }
}
