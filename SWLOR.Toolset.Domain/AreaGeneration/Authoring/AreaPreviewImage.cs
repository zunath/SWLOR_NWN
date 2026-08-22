namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Top-left, row-major RGBA preview pixels.</summary>
    public sealed record AreaPreviewImage(int Width, int Height, byte[] Pixels, int MissingTileGraphics);
}
