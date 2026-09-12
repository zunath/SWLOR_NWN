using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration;

/// <summary>Interpolates the resolved tile's oriented corner heights in world meters.</summary>
internal static class ResolvedGround
{
    internal static float HeightAt(ResolvedLayout layout, TilesetModel tileset, float x, float y)
    {
        var tileX = Math.Clamp((int)MathF.Floor(x / 10), 0, layout.Width - 1);
        var tileY = Math.Clamp((int)MathF.Floor(y / 10), 0, layout.Height - 1);
        var resolved = layout.GetTile(tileX, tileY);
        var tile = tileset.Tiles[resolved.TileId];
        var localX = Math.Clamp((x - tileX * 10) / 10, 0, 1);
        var localY = Math.Clamp((y - tileY * 10) / 10, 0, 1);
        var topLeft = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.TopLeft);
        var topRight = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.TopRight);
        var bottomRight = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.BottomRight);
        var bottomLeft = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.BottomLeft);
        var bottom = bottomLeft + (bottomRight - bottomLeft) * localX;
        var top = topLeft + (topRight - topLeft) * localX;
        var offset = bottom + (top - bottom) * localY;
        return (resolved.Height + offset) * layout.HeightTransition;
    }
}
