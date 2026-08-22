using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A saved blueprint's preview must not sit stale on an already-visible palette tile.
    /// <see cref="ThumbnailService.Invalidate"/> only drops its own memory/disk caches and an in-flight
    /// render; nothing told a tile that already has a delivered preview - or is mid-render - to ask
    /// again, so appearance and icon edits stayed invisible until the category was closed and reopened.
    /// </summary>
    [NonParallelizable]
    public class PalettePreviewInvalidationTests
    {
        [AvaloniaTest]
        public void SavingAnOpenBlueprintRefreshesItsVisibleTile()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var source = new CountingRenderSource();
            var thumbnails = new ThumbnailService(workspace, source);
            var palette = new PaletteViewModel(
                workspace, new CategoryService(workspace, log), log, thumbnails: thumbnails);

            // A tile already realized and holding a delivered preview, the way one sitting on screen
            // does after its first scroll into view.
            var tile = new PaletteTileViewModel("test_item", "Test Item", null, PaletteSource.Custom);
            palette.Tiles.Add(tile);
            palette.EnsurePreview(tile);
            Drain();

            var firstPreview = tile.Preview;
            firstPreview.Should().NotBeNull();
            source.RenderCalls.Should().Be(1);

            thumbnails.Invalidate(palette.SelectedType, "test_item");
            Drain();

            source.RenderCalls.Should().Be(2,
                "the visible tile must ask again immediately rather than wait for the category to be closed and reopened");
            tile.Preview.Should().NotBeSameAs(firstPreview, "the stale bitmap must not be left in place");
        }

        [AvaloniaTest]
        public void InvalidatingADifferentTypeLeavesTheTileAlone()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var source = new CountingRenderSource();
            var thumbnails = new ThumbnailService(workspace, source);
            var palette = new PaletteViewModel(
                workspace, new CategoryService(workspace, log), log, thumbnails: thumbnails);

            var tile = new PaletteTileViewModel("test_item", "Test Item", null, PaletteSource.Custom);
            palette.Tiles.Add(tile);
            palette.EnsurePreview(tile);
            Drain();
            source.RenderCalls.Should().Be(1);

            // A different resource type invalidating under the same resref must not disturb a tile that
            // belongs to the palette's current (different) selected type.
            var otherType = palette.SelectedType == ResourceType.Utp ? ResourceType.Utc : ResourceType.Utp;
            thumbnails.Invalidate(otherType, "test_item");
            Drain();

            source.RenderCalls.Should().Be(1, "an unrelated type's invalidation must not trigger a re-render");
        }

        /// <summary>
        /// Lets the render task finish and its UI-thread callback run. The service does its work on
        /// the pool and publishes through the dispatcher, so a test has to pump both.
        /// </summary>
        private static void Drain()
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
                Dispatcher.UIThread.RunJobs();
            }
        }

        private sealed class CountingRenderSource : IPreviewImageSource
        {
            public int RenderCalls;

            public bool IsAvailable => true;

            public DateTime ContentVersionUtc => new(2026, 1, 1);

            public IconImage? Render(ResourceType type, string resRef, bool useIndexedBlueprint = false)
            {
                Interlocked.Increment(ref RenderCalls);
                return new IconImage(2, 2, new byte[2 * 2 * 4]);
            }

            public IconImage? RenderModel(string modelResRef, bool renderDoorTransitionFallback = false) => null;

            public IconImage? RenderTileGroup(IReadOnlyList<string> slotModelResRefs, int columns, int rows) => null;

            public IconImage? RenderCreatureAppearance(int appearanceId) => null;
        }
    }
}
