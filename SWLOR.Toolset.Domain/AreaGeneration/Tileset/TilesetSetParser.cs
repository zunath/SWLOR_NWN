#nullable disable
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.AreaGeneration.Tileset
{
    /// <summary>
    /// Adapts the toolset's canonical <see cref="SetFileParser"/> model to the compact model used by
    /// the procedural solver. Keeping parsing in one place means generation inherits the same corrupt
    /// count handling, door-block discovery, and corpus fixes as terrain painting and area rendering.
    /// </summary>
    public static class TilesetSetParser
    {
        public static TilesetModel Parse(string resref, string setFileContents)
        {
            if (string.IsNullOrWhiteSpace(resref))
                throw new ArgumentException("A tileset resref is required.", nameof(resref));

            return FromDefinition(resref, SetFileParser.Parse(setFileContents));
        }

        public static TilesetModel FromDefinition(string resref, TilesetDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var model = new TilesetModel
            {
                Resref = resref,
                Name = definition.Name,
                IsInterior = definition.Interior,
                HasHeightTransition = definition.HasHeightTransition,
                HeightTransition = definition.Transition,
                BorderTerrain = definition.Border,
                DefaultTerrain = definition.Default,
                FloorTerrain = definition.Floor,
                Terrains = definition.Terrains.Select(terrain => terrain.Name).ToList(),
                Crossers = definition.Crossers.Select(crosser => crosser.Name).ToList()
            };

            for (var tileId = 0; tileId < definition.Tiles.Count; tileId++)
            {
                var tile = definition.Tiles[tileId];
                model.Tiles.Add(new TileRecord
                {
                    TileId = tileId,
                    Model = tile.Model,
                    WalkMesh = tile.WalkMesh,
                    PathNode = tile.PathNode,
                    ImageMap2D = tile.ImageMap2D ?? string.Empty,
                    Corners =
                    [
                        tile.TopLeft,
                        tile.TopRight,
                        tile.BottomRight,
                        tile.BottomLeft
                    ],
                    CornerHeights =
                    [
                        tile.TopLeftHeight,
                        tile.TopRightHeight,
                        tile.BottomRightHeight,
                        tile.BottomLeftHeight
                    ],
                    Edges = [tile.Top, tile.Right, tile.Bottom, tile.Left],
                    Doors = tile.Doors.Select(door => new TileDoorRecord
                    {
                        Type = door.Type,
                        X = (float)door.X,
                        Y = (float)door.Y,
                        Z = (float)door.Z,
                        Orientation = (float)door.Orientation
                    }).ToList()
                });
            }

            for (var groupIndex = 0; groupIndex < definition.Groups.Count; groupIndex++)
            {
                var group = definition.Groups[groupIndex];
                model.Groups.Add(new TileGroupRecord
                {
                    Name = group.Name,
                    Rows = group.Rows,
                    Columns = group.Columns,
                    TileIds = group.TileIndices.ToList()
                });

                foreach (var tileId in group.TileIndices)
                {
                    if (tileId >= 0 && tileId < model.Tiles.Count && model.Tiles[tileId].GroupIndex < 0)
                        model.Tiles[tileId].GroupIndex = groupIndex;
                }
            }

            return model;
        }
    }
}
