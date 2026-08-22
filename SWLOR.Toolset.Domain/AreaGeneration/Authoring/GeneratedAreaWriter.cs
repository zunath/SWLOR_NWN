#nullable enable
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Creates a solved procedural draft as a normal area in the open module.</summary>
    public static class GeneratedAreaWriter
    {
        public static bool TryCreate(
            ModuleWorkspace workspace,
            TilesetCatalog tilesets,
            AreaGenerationDraft draft,
            string resref,
            string displayName,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(tilesets);
            ArgumentNullException.ThrowIfNull(draft);

            if (!draft.Result.Success || draft.Result.Resolved == null)
            {
                error = string.IsNullOrWhiteSpace(draft.Result.FailureReason)
                    ? "Generate a successful preview before creating the area."
                    : draft.Result.FailureReason;
                return false;
            }

            NewAreaWriter.TilesetResolver resolver = tilesets.TryGetTileset;
            return NewAreaWriter.TryCreate(
                workspace,
                resolver,
                resref,
                displayName,
                draft.Composition.Tileset.TilesetResref,
                draft.Result.Resolved.Width,
                draft.Result.Resolved.Height,
                (are, git, gic) =>
                    GeneratedAreaDocumentPopulator.Populate(draft, workspace, are, git, gic),
                out error);
        }
    }
}
