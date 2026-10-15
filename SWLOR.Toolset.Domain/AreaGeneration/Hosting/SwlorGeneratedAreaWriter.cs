using Nwn.Authoring.Areas.Creation;
using Nwn.Authoring.Areas.Generation.Hosting;
using Serilog;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>Creates a solved procedural draft as a normal area in the open module.</summary>
public sealed class SwlorGeneratedAreaWriter : IGeneratedAreaWriter
{
    private static readonly ILogger Logger = Log.ForContext<SwlorGeneratedAreaWriter>();

    private readonly ModuleWorkspace _workspace;
    private readonly TilesetCatalog _tilesets;

    public SwlorGeneratedAreaWriter(ModuleWorkspace workspace, TilesetCatalog tilesets)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
    }

    public bool TryCreate(GeneratedAreaRequest request, out string error)
    {
        ArgumentNullException.ThrowIfNull(request);
        var draft = request.Draft;
        if (!draft.Result.Success || draft.Result.Resolved == null)
        {
            error = string.IsNullOrWhiteSpace(draft.Result.FailureReason)
                ? "Generate a successful preview before creating the area."
                : draft.Result.FailureReason;
            return false;
        }

        AreaTilesetResolver resolver = _tilesets.TryGetTileset;
        Logger.Information(
            "Creating generated area {AreaResref} from a {Width}x{Height} solved layout.",
            request.ResRef,
            draft.Result.Resolved.Width,
            draft.Result.Resolved.Height);
        var created = NewAreaWriter.TryCreate(
            _workspace,
            resolver,
            request.ResRef,
            request.DisplayName,
            draft.Composition.Tileset.TilesetResref,
            draft.Result.Resolved.Width,
            draft.Result.Resolved.Height,
            request.Populate,
            out error);

        if (created)
            Logger.Information("Created generated area {AreaResref}.", request.ResRef);
        else
            Logger.Warning("Could not create generated area {AreaResref}: {Error}", request.ResRef, error);

        return created;
    }
}
