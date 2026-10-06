using Avalonia.Controls;
using Avalonia.Platform;
using Nwn.Toolset.Avalonia.Areas.Generation;
using SWLOR.Toolset.Domain.AreaGeneration.Hosting;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Builds the shared Area Generator's host from SWLOR's content, workspace and game data.</summary>
public static class SwlorAreaGeneratorHost
{
    private const string ApplicationName = "SWLOR Toolset";
    private const string IconUri = "avares://SWLOR.Toolset/Assets/swlor-logo.png";

    public static AreaGeneratorHost Create(
        ModuleWorkspace workspace,
        TilesetCatalog tilesets,
        ResourceIndex? resources) =>
        new(SwlorAreaGenerationCatalog.Create(),
            new SwlorTilesetSource(tilesets),
            new SwlorGeneratedAreaWriter(workspace, tilesets))
        {
            Blueprints = new SwlorBlueprintSource(workspace),
            PopulationPolicy = new SwlorPopulationPolicy(),
            TileGraphics = resources == null ? null : new SwlorPreviewTileGraphics(resources),
            Log = new SwlorAreaGenerationLog(),
            Window = new AreaGeneratorWindowOptions(ApplicationName, LoadIcon())
        };

    private static WindowIcon? LoadIcon()
    {
        try
        {
            return new WindowIcon(AssetLoader.Open(new Uri(IconUri)));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
