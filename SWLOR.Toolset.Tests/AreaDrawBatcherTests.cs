using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP4.5 <see cref="AreaDrawBatcher"/>: grouping tile placements into one
    /// batch per distinct shared <see cref="RenderModel"/> reference, with fallback placements
    /// collected into their own trailing null-model batch.
    /// </summary>
    public class AreaDrawBatcherTests
    {
        private static RenderModel MakeModel(string name) =>
            new() { Name = name, Meshes = Array.Empty<RenderMesh>() };

        private static TilePlacement MakePlacement(int index, RenderModel? model, bool isFallback) => new()
        {
            TileIndex = index,
            Column = index,
            Row = 0,
            TileId = index,
            Orientation = 0,
            HeightLevel = 0,
            CenterX = index * 10f + 5f,
            CenterY = 5f,
            HeightOffset = 0f,
            Transform = Matrix4x4.Identity,
            ModelResRef = model?.Name,
            Model = model,
            IsFallback = isFallback
        };

        [Test]
        public void GroupByModel_AllPlacementsShareOneModel_ProducesSingleBatch()
        {
            var model = MakeModel("tile_a");
            var tiles = new[]
            {
                MakePlacement(0, model, false),
                MakePlacement(1, model, false),
                MakePlacement(2, model, false)
            };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().HaveCount(1);
            batches[0].Model.Should().BeSameAs(model);
            batches[0].Placements.Should().HaveCount(3);
        }

        [Test]
        public void GroupByModel_DistinctModelReferences_ProduceSeparateBatchesEvenWithSameName()
        {
            // Two different RenderModel instances that happen to share a Name must still batch
            // separately - grouping is by reference identity (the whole point of caching via
            // TileModelCache), not by any value-equality on the model's contents.
            var modelA = MakeModel("same_name");
            var modelB = MakeModel("same_name");

            var tiles = new[]
            {
                MakePlacement(0, modelA, false),
                MakePlacement(1, modelB, false)
            };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().HaveCount(2);
            batches.Select(b => b.Model).Should().Contain(new[] { modelA, modelB });
        }

        [Test]
        public void GroupByModel_MultipleModels_PreservesFirstAppearanceOrder()
        {
            var modelA = MakeModel("a");
            var modelB = MakeModel("b");

            var tiles = new[]
            {
                MakePlacement(0, modelB, false),
                MakePlacement(1, modelA, false),
                MakePlacement(2, modelB, false),
                MakePlacement(3, modelA, false)
            };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().HaveCount(2);
            batches[0].Model.Should().BeSameAs(modelB, "modelB appeared first in the input");
            batches[1].Model.Should().BeSameAs(modelA);
            batches[0].Placements.Should().HaveCount(2);
            batches[1].Placements.Should().HaveCount(2);
        }

        [Test]
        public void GroupByModel_FallbackPlacements_GroupIntoTrailingNullModelBatch()
        {
            var model = MakeModel("real");
            var tiles = new[]
            {
                MakePlacement(0, model, false),
                MakePlacement(1, null, true),
                MakePlacement(2, model, false),
                MakePlacement(3, null, true)
            };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().HaveCount(2);
            batches[0].Model.Should().BeSameAs(model);
            batches[0].Placements.Should().HaveCount(2);
            batches[1].Model.Should().BeNull();
            batches[1].Placements.Should().HaveCount(2);
        }

        [Test]
        public void GroupByModel_PlacementFlaggedFallbackDespiteNonNullModel_StillRoutesToFallbackBatch()
        {
            // IsFallback is authoritative even if a stale Model reference is somehow present -
            // the renderer must draw the placeholder cube, not the (possibly wrong) mesh.
            var model = MakeModel("stale");
            var tiles = new[] { MakePlacement(0, model, isFallback: true) };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().ContainSingle();
            batches[0].Model.Should().BeNull();
        }

        [Test]
        public void GroupByModel_EmptyInput_ReturnsNoBatches()
        {
            AreaDrawBatcher.GroupByModel(Array.Empty<TilePlacement>()).Should().BeEmpty();
        }

        [Test]
        public void GroupByModel_NoFallbacks_OmitsFallbackBatchEntirely()
        {
            var model = MakeModel("only");
            var tiles = new[] { MakePlacement(0, model, false) };

            var batches = AreaDrawBatcher.GroupByModel(tiles);

            batches.Should().OnlyContain(b => b.Model != null);
        }
    }
}
