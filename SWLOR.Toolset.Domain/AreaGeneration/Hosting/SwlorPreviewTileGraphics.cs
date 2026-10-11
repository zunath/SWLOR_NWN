using Nwn.Authoring.Areas.Generation.Hosting;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>Supplies each tile's 2D map picture from the indexed game resources.</summary>
public sealed class SwlorPreviewTileGraphics : IAreaPreviewTileGraphics
{
    private readonly ResourceIndex _resources;

    public SwlorPreviewTileGraphics(ResourceIndex resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public bool TryLoadTileImage(string imageMap2D, out AreaPreviewTexture texture)
    {
        var image = TextureLoader.LoadTga(_resources, imageMap2D);
        if (image == null)
        {
            texture = null!;
            return false;
        }

        texture = new AreaPreviewTexture(image.Width, image.Height, image.Pixels);
        return true;
    }
}
