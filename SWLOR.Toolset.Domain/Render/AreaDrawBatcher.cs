namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Groups an <see cref="AreaScene"/>'s tile placements into per-<see cref="RenderModel"/> draw
    /// batches, so a renderer uploads each distinct model's geometry to the GPU exactly once (see
    /// <see cref="TileModelCache"/>, which already guarantees placements sharing a model resref
    /// share the same <see cref="RenderModel"/> instance) and then issues one draw call per
    /// placement referencing it. Fallback placements (missing/unparseable model) are collected into
    /// their own batch with a null <see cref="TileBatch.Model"/>, signaling "draw a placeholder"
    /// to the renderer instead of a real mesh. Pure data grouping - no GL dependency.
    /// </summary>
    public static class AreaDrawBatcher
    {
        /// <summary>One draw batch: every placement in <see cref="Placements"/> shares <see cref="Model"/> (or all are fallbacks when <see cref="Model"/> is null).</summary>
        public sealed class TileBatch
        {
            /// <summary>The shared render geometry every placement in this batch references, or null for the fallback batch.</summary>
            public RenderModel? Model { get; init; }

            public required IReadOnlyList<TilePlacement> Placements { get; init; }
        }

        /// <summary>
        /// Groups <paramref name="tiles"/> by <see cref="TilePlacement.Model"/> reference identity
        /// (default object equality - deliberately not a value comparison, since the point is to
        /// detect placements that already share the exact same cached <see cref="RenderModel"/>
        /// instance). Batch order follows each model's first appearance in
        /// <paramref name="tiles"/> for deterministic output; the fallback batch (present only when
        /// at least one fallback placement exists) is always last.
        /// </summary>
        public static IReadOnlyList<TileBatch> GroupByModel(IReadOnlyList<TilePlacement> tiles)
        {
            ArgumentNullException.ThrowIfNull(tiles);

            var order = new List<RenderModel>();
            var groups = new Dictionary<RenderModel, List<TilePlacement>>();
            var fallback = new List<TilePlacement>();

            foreach (var tile in tiles)
            {
                if (tile.IsFallback || tile.Model == null)
                {
                    fallback.Add(tile);
                    continue;
                }

                if (!groups.TryGetValue(tile.Model, out var list))
                {
                    list = new List<TilePlacement>();
                    groups[tile.Model] = list;
                    order.Add(tile.Model);
                }

                list.Add(tile);
            }

            var batches = new List<TileBatch>(order.Count + 1);
            foreach (var model in order)
                batches.Add(new TileBatch { Model = model, Placements = groups[model] });

            if (fallback.Count > 0)
                batches.Add(new TileBatch { Model = null, Placements = fallback });

            return batches;
        }
    }
}
