using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using Nwn.Preview.Icons;
using Nwn.Toolset.Avalonia.Palettes;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>A saved blueprint invalidates the preview held by the active shared palette entry.</summary>
    [NonParallelizable]
    public class PalettePreviewInvalidationTests
    {
        private string _testRoot = string.Empty;
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "swlor-palette-preview-" + Guid.NewGuid().ToString("N"));
            _moduleRoot = Path.Combine(_testRoot, "Module");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));
            File.WriteAllBytes(
                Path.Combine(_moduleRoot, "utp", "test_item.utp.json"),
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Utp, "test_item", "Test Item"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRoot))
                ScratchDirectory.Delete(_testRoot);
        }

        [AvaloniaTest]
        public void SavingAnOpenBlueprintRefreshesItsVisibleTile()
        {
            var (palette, thumbnails, source) = CreatePalette();
            var entry = SelectUnsortedEntry(palette);
            palette.PresentationState.EnsurePreview(entry);
            Drain();

            var firstPreview = entry.Preview;
            firstPreview.Should().NotBeNull();
            source.RenderCalls.Should().Be(1);

            thumbnails.Invalidate(palette.SelectedType, "test_item");
            Drain();

            source.RenderCalls.Should().Be(2,
                "the active shared entry must request a fresh preview after invalidation");
            entry.Preview.Should().NotBeSameAs(firstPreview, "the stale bitmap must not be left in place");
        }

        [AvaloniaTest]
        public void InvalidatingADifferentTypeLeavesTheTileAlone()
        {
            var (palette, thumbnails, source) = CreatePalette();
            var entry = SelectUnsortedEntry(palette);
            palette.PresentationState.EnsurePreview(entry);
            Drain();
            source.RenderCalls.Should().Be(1);

            var otherType = palette.SelectedType == ResourceType.Utp ? ResourceType.Utc : ResourceType.Utp;
            thumbnails.Invalidate(otherType, "test_item");
            Drain();

            source.RenderCalls.Should().Be(1,
                "an unrelated type's invalidation must not trigger a re-render");
        }

        private (PaletteViewModel Palette, ThumbnailService Thumbnails, CountingRenderSource Source) CreatePalette()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.OpenAndSettle(_moduleRoot);
            var source = new CountingRenderSource();
            var thumbnails = new ThumbnailService(workspace, source);
            var palette = new PaletteViewModel(
                workspace, new CategoryService(workspace, log), log, thumbnails: thumbnails);
            palette.Refresh();
            return (palette, thumbnails, source);
        }

        private static PaletteEntryRow SelectUnsortedEntry(PaletteViewModel palette)
        {
            var unsorted = palette.PresentationState.Rows.Single(row => row.Name == "Unsorted");
            palette.PresentationState.SelectedRow = unsorted;
            return palette.PresentationState.Tiles.Single(entry => entry.ResRef == "test_item");
        }

        /// <summary>Lets thumbnail work finish and its UI-thread callback run.</summary>
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
