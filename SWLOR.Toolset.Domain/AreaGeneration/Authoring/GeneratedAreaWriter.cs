#nullable enable
using Serilog;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Creates a solved procedural draft as a normal area in the open module.</summary>
    public sealed class GeneratedAreaWriter
    {
        private static readonly ILogger Logger = Log.ForContext<GeneratedAreaWriter>();

        private GeneratedAreaWriter()
        {
        }

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
            Logger.Information(
                "Creating generated area {AreaResref} from a {Width}x{Height} solved layout.",
                resref,
                draft.Result.Resolved.Width,
                draft.Result.Resolved.Height);
            var created = NewAreaWriter.TryCreate(
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

            if (created)
                Logger.Information("Created generated area {AreaResref}.", resref);
            else
                Logger.Warning("Could not create generated area {AreaResref}: {Error}", resref, error);

            return created;
        }
    }
}
