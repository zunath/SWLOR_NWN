using Nwn.Authoring.Areas.Creation;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.Workspace;

/// <summary>SWLOR module-layout adapter for the shared transactional area creator.</summary>
public static class NewAreaWriter
{
    public const string TemplateResRef = "area_template";
    public const int MaxDimension = AreaCreationWriter.MaximumDimension;

    public static bool TryCreate(
        ModuleWorkspace workspace,
        AreaTilesetResolver? resolveTileset,
        string resRef,
        string displayName,
        string tilesetResRef,
        int width,
        int height,
        out string error) =>
        TryCreate(workspace, resolveTileset, resRef, displayName, tilesetResRef, width, height, null, out error);

    public static bool TryCreate(
        ModuleWorkspace workspace,
        AreaTilesetResolver? resolveTileset,
        string resRef,
        string displayName,
        string tilesetResRef,
        int width,
        int height,
        AreaDocumentPopulator? populate,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var canonicalResRef = (resRef ?? string.Empty).Trim().ToLowerInvariant();
        var paths = new AreaCreationPaths(
            workspace.ModuleRoot,
            TemplateResRef,
            workspace.GetResourcePath(ResourceType.Area, TemplateResRef),
            Path.Combine(workspace.ModuleRoot, "git", TemplateResRef + ".git.json"),
            Path.Combine(workspace.ModuleRoot, "gic", TemplateResRef + ".gic.json"),
            workspace.GetResourcePath(ResourceType.Area, canonicalResRef),
            Path.Combine(workspace.ModuleRoot, "git", canonicalResRef + ".git.json"),
            Path.Combine(workspace.ModuleRoot, "gic", canonicalResRef + ".gic.json"),
            Path.Combine(workspace.ModuleRoot, "ifo", "module.ifo.json"),
            SwlorAreaCreationMarker.Prefix);
        return AreaCreationWriter.TryCreate(paths, new NimGffDocumentCodec(), resolveTileset,
            canonicalResRef, displayName, tilesetResRef, width, height, populate, out error);
    }
}
