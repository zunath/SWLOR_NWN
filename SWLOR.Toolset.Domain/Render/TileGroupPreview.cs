using System.Numerics;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Assembles a multi-tile palette group into one <see cref="RenderModel"/> laid out on the grid,
    /// so its thumbnail shows the shape a builder is actually about to stamp.
    /// </summary>
    /// <remarks>
    /// A group's preview used to be its first tile's model, which for a 1x2 road or a 2x2 ruin drew
    /// a single square that looked exactly like every other single square in the palette - the
    /// footprint was in the subtitle and nowhere else. Composing is cheap: NWN tile models are
    /// origin-centred over their own 10m cell, so each tile only needs a translation onto its cell,
    /// and the mesh arrays are shared by reference rather than copied - only the per-mesh transform
    /// differs.
    /// </remarks>
    public static class TileGroupPreview
    {
        /// <summary>
        /// One model per footprint slot, ROW-MAJOR over <paramref name="columns"/> (a null slot is a
        /// hole in the group, or a tile whose model would not resolve), composed into a single model
        /// centred on the footprint. Null when nothing in the group has geometry.
        /// </summary>
        public static RenderModel? Compose(
            IReadOnlyList<RenderModel?> slotModels, int columns, int rows)
        {
            ArgumentNullException.ThrowIfNull(slotModels);

            if (columns <= 0 || rows <= 0)
                return null;

            // Centre the footprint on the origin so the thumbnail camera frames it the same way it
            // frames a single tile, rather than looking at a group that drifts off with its size.
            var originX = (columns - 1) * AreaSceneBuilder.TileSize / 2f;
            var originY = (rows - 1) * AreaSceneBuilder.TileSize / 2f;

            var meshes = new List<RenderMesh>();
            for (var slot = 0; slot < slotModels.Count && slot < columns * rows; slot++)
            {
                if (slotModels[slot] is not { } model)
                    continue;

                var column = slot % columns;
                var row = slot / columns;
                var placement = Matrix4x4.CreateTranslation(
                    column * AreaSceneBuilder.TileSize - originX,
                    row * AreaSceneBuilder.TileSize - originY,
                    0f);

                foreach (var mesh in model.Meshes)
                {
                    meshes.Add(new RenderMesh
                    {
                        NodeName = mesh.NodeName,
                        TextureName = mesh.TextureName,
                        Positions = mesh.Positions,
                        Normals = mesh.Normals,
                        TexCoords = mesh.TexCoords,
                        Indices = mesh.Indices,
                        DiffuseColor = mesh.DiffuseColor,
                        TileFade = mesh.TileFade,
                        Transform = mesh.Transform * placement
                    });
                }
            }

            return meshes.Count == 0 ? null : new RenderModel { Name = "group", Meshes = meshes };
        }
    }
}
