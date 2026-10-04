#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Tilesets;
using SharedTileResolver = Nwn.Authoring.Areas.Generation.TileResolver;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    public static class TileResolver
    {
        public static bool TryResolve(
            TilesetModel tileset,
            MacroLayout layout,
            Random random,
            out ResolvedLayout resolved,
            out string failureReason)
        {
            var protectedCells = BuildProtectedFeatureCells(tileset, layout);
            return SharedTileResolver.TryResolve(tileset, layout, random, out resolved, out failureReason, protectedCells);
        }

        public static SharedTileResolver.HeightAwareProbeCache BuildHeightAwareProbeCache(
            TilesetModel tileset,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null,
            IReadOnlyCollection<int> excludedTiles = null) =>
            SharedTileResolver.BuildHeightAwareProbeCache(tileset, extraDoorSlotCrossers, excludedTiles);

        private static IReadOnlyCollection<(int X, int Y)> BuildProtectedFeatureCells(TilesetModel tileset, MacroLayout layout)
        {
            if (layout.FeatureTiles.Count == 0 || layout.Rooms.Count == 0)
                return Array.Empty<(int X, int Y)>();

            var surfaceLayout = new ResolvedLayout
            {
                Width = layout.Corners.Width,
                Height = layout.Corners.Height,
                Rooms = layout.Rooms,
                Transitions = layout.Transitions,
                CornerTerrains = layout.Corners,
                OpenTerrain = layout.OpenTerrain,
                SecondaryOpenTerrain = layout.SecondaryOpenTerrain,
                Crossers = layout.Crossers,
                StampedStructureTiles = layout.StampedOpenSetPieceFootprints.SelectMany(footprint => footprint).ToHashSet()
            };
            var surface = Decoration.DecorationPlacementSafety.BuildOpenSurface(surfaceLayout);
            var protectedCells = new HashSet<(int X, int Y)>();
            foreach (var route in Decoration.DecorationPlacementSafety.BuildRoutes(surfaceLayout, surface, string.Empty))
            {
                protectedCells.Add(((int)MathF.Floor(route.Start.X / 10), (int)MathF.Floor(route.Start.Y / 10)));
                protectedCells.Add(((int)MathF.Floor(route.End.X / 10), (int)MathF.Floor(route.End.Y / 10)));
            }
            return protectedCells;
        }
    }
}
