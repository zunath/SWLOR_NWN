using Avalonia.Headless.NUnit;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The searchable appearance grid, shared by every editor that picks one.
    /// </summary>
    /// <remarks>
    /// Exercised without a <c>ThumbnailService</c>: the grid must page, filter, and record a pick
    /// whether or not previews are available, because that is exactly the state it is in for the
    /// first second after a tab opens and permanently when the game data did not resolve.
    /// </remarks>
    [TestFixture]
    public class AppearanceGalleryTests
    {
        [Test]
        public void OnlyTheFirstPageIsPublishedUntilSomethingAsksForMore()
        {
            var section = Section(Options(500), out _);

            section.Tiles.Should().HaveCount(48);
            section.CanLoadMore.Should().BeTrue();
            section.MatchSummary.Should().Be("48 of 500 appearances");

            section.LoadMoreCommand.Execute(null);
            section.Tiles.Should().HaveCount(96);

            for (var page = 0; page < 20; page++)
                section.LoadMoreCommand.Execute(null);

            section.Tiles.Should().HaveCount(500);
            section.CanLoadMore.Should().BeFalse();
            section.MatchSummary.Should().Be("500 appearances");
        }

        [Test]
        public void ClearingTheSearchTakesEffectAtOnceRatherThanWaitingOutTheDebounce()
        {
            // Emptying the box is a search being abandoned, not one being typed. Waiting leaves the
            // old results sitting there looking like the filter stuck.
            var section = Section(Options(200), out _);

            section.Query = "   ";
            section.Tiles.Should().HaveCount(48);
            section.MatchSummary.Should().Be("48 of 200 appearances");
        }

        [Test]
        public void PickingATileRecordsTheEditAndMovesTheCurrentMarker()
        {
            var section = Section(Options(100), out var applied);

            var target = section.Tiles[7];
            section.Highlighted = target;

            applied.Should().ContainSingle().Which.Key.Should().Be(target.Option.Key);
            target.IsCurrent.Should().BeTrue();
            section.Tiles.Where(tile => tile.IsCurrent).Should().ContainSingle();
        }

        [Test]
        public void ARefusedPickPutsTheGridBackWhereItWas()
        {
            var options = Options(100);
            var section = new AppearanceGallerySectionViewModel(
                options,
                thumbnails: null,
                currentKey: () => "0",
                apply: _ => false,
                noun: "appearance");

            section.Highlighted = section.Tiles[9];

            section.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("0");
            section.Highlighted.Should().BeNull("a refused edit clears the highlight it came from");
        }

        [Test]
        public void PickingWhatIsAlreadyStoredIsNotAnEdit()
        {
            var section = Section(Options(20), out var applied);

            section.Highlighted = section.Tiles.Single(tile => tile.Option.Key == "0");

            applied.Should().BeEmpty();
        }

        [Test]
        public void AStoredRowTheTableDoesNotHaveIsReportedRatherThanHidden()
        {
            var unknown = new AppearanceGallerySectionViewModel(
                Options(10),
                thumbnails: null,
                currentKey: () => "9999",
                apply: _ => true,
                noun: "appearance");

            unknown.CurrentIsUnknown.Should().BeTrue();
            unknown.CurrentDescription.Should().Contain("9999");

            var known = Section(Options(10), out _);
            known.CurrentIsUnknown.Should().BeFalse();
            known.CurrentDescription.Should().Contain("Appearance 0");
        }

        [Test]
        public void TheTileFallsBackToALetterUntilItsPictureArrives()
        {
            var section = Section(Options(3), out _);

            section.Tiles[0].Preview.Should().BeNull("no thumbnail service was supplied");
            section.Tiles[0].Glyph.Should().Be("A");
            section.Tiles[0].HasDetail.Should().BeTrue();
        }

        [AvaloniaTest, NonParallelizable]
        public void VirtualizedTilesRecycleWithoutBindingErrors()
        {
            var previous = Logger.Sink;
            var sink = new BindingLogSink();
            Logger.Sink = sink;
            using var section = new AppearanceGallerySectionViewModel(
                Options(100), null, () => "0", _ => true, noun: "appearance", tileSize: 92);
            var window = new Window
            {
                Width = 700,
                Height = 500,
                Content = new AppearanceGalleryView { DataContext = section }
            };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                section.Tiles[0].TileSize.Should().Be(92);
                section.Tiles[0].TileImageHeight.Should().BeApproximately(67.16, 0.001);

                // Replacing the page recycles realized cells. Their sizing values must stay on the
                // item instead of walking through the ListBox data context while it is being cleared.
                section.SetOptions(Options(12));
                Dispatcher.UIThread.RunJobs();
                section.SetOptions(Options(100));
                Dispatcher.UIThread.RunJobs();
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
                Logger.Sink = previous;
            }

            sink.Errors.Should().BeEmpty();
        }

        [AvaloniaTest]
        public void VisiblePreviewsRetryAfterGameResourcesBecomeAvailable()
        {
            var source = new AppearancePreviewSource { IsAvailable = false };
            var thumbnails = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);
            using var section = new AppearanceGallerySectionViewModel(
                Options(3), thumbnails, () => "0", _ => true, noun: "appearance");

            foreach (var tile in section.Tiles)
                section.EnsurePreview(tile);

            source.AppearanceCalls.Should().Be(0,
                "opening during game-data loading cannot render yet");
            section.Tiles.Should().OnlyContain(tile => tile.Preview == null);

            source.IsAvailable = true;
            section.ReloadPreviews();
            DrainDispatcher();

            source.AppearanceCalls.Should().Be(3,
                "only the currently published page is retried");
            section.Tiles.Should().OnlyContain(tile => tile.Preview != null);
        }

        [AvaloniaTest]
        public void PublishedTilesWaitForViewportRealizationBeforeRendering()
        {
            var source = new AppearancePreviewSource { IsAvailable = true };
            var thumbnails = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);
            using var section = new AppearanceGallerySectionViewModel(
                Options(100), thumbnails, () => "0", _ => true, noun: "appearance");

            source.AppearanceCalls.Should().Be(0,
                "publishing a page must not render cells outside the virtualized viewport");

            section.EnsurePreview(section.Tiles[0]);
            section.EnsurePreview(section.Tiles[1]);
            section.EnsurePreview(section.Tiles[0]);
            DrainDispatcher();

            source.AppearanceCalls.Should().Be(2,
                "only realized cells render and repeated realization does not duplicate work");
            section.Tiles.Take(2).Should().OnlyContain(tile => tile.Preview != null);
            section.Tiles.Skip(2).Should().OnlyContain(tile => tile.Preview == null);
        }

        [Test]
        public void DisposingCancelsAPendingSearchRatherThanLettingItFire()
        {
            var section = Section(Options(50), out _);

            section.Query = "Appearance 4";
            section.Dispose();

            // Disposing twice is a no-op, which matters because the editor disposes on close and
            // the tab may already have been torn down.
            section.Dispose();
        }

        [Test]
        public void OneGridServesEveryEditorThatPicksAnAppearance()
        {
            // The door editor and the creature editor draw the same control. They had arrived at
            // the same design separately, and the creature editor had not arrived at it at all.
            var doorView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "DoorEditorView.axaml"));
            var blueprintView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "BlueprintEditorView.axaml"));
            var creatureView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            doorView.Should().Contain("<appearance:AppearanceGalleryView");
            blueprintView.Should().Contain("appearance:AppearanceGallerySectionViewModel");
            creatureView.Should().Contain("<appearance:AppearanceGalleryView");
            creatureView.Should().Contain("<behaviors:BehaviorRowView />",
                "creature equipment reuses the shared progressive choice control");
            var itemView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "ItemEditorView.axaml"));
            itemView.Should().Contain("<items:PaletteColorPickerView");
            creatureView.Should().Contain("<items:PaletteColorPickerView",
                "creature colors reuse the item editor's established palette control");
            creatureView.Should().Contain("<TabItem Header=\"Equipment\"");
            creatureView.Should().Contain("SelectedItem=\"{Binding EquipmentSlots.SelectedSlot, Mode=TwoWay}\"",
                "equipment reuses the merchant editor's focused rail/work-pane interaction");
            var appearanceView = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Appearance", "AppearanceGalleryView.axaml"));
            appearanceView.Should().Contain("<controls:VirtualizingWrapPanel />");
            appearanceView.Should().Contain("Loaded=\"OnTileLoaded\"",
                "appearance previews must follow the palette's viewport-driven loading pattern");

            Directory.Exists(Path.Combine(
                    CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Appearance"))
                .Should().BeTrue();
            File.Exists(Path.Combine(
                    CorpusLocator.RepositoryRoot,
                    "SWLOR.Toolset", "Editors", "Doors", "DoorAppearanceSectionViewModel.cs"))
                .Should().BeFalse("the door editor uses the shared grid now");
        }

        private static AppearanceGallerySectionViewModel Section(
            IReadOnlyList<AppearanceOption> options,
            out List<AppearanceOption> applied)
        {
            var picks = new List<AppearanceOption>();
            var current = "0";
            var section = new AppearanceGallerySectionViewModel(
                options,
                thumbnails: null,
                currentKey: () => current,
                apply: option =>
                {
                    picks.Add(option);
                    current = option.Key;
                    return true;
                },
                noun: "appearance");

            applied = picks;
            return section;
        }

        private static IReadOnlyList<AppearanceOption> Options(int count) =>
            Enumerable.Range(0, count)
                .Select(index => new AppearanceOption(
                    index.ToString(),
                    $"Appearance {index}",
                    $"row {index} · label_{index}",
                    CreatureAppearanceId: index))
                .ToList();

        private static void DrainDispatcher()
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
                Dispatcher.UIThread.RunJobs();
            }
        }

        private sealed class AppearancePreviewSource : IPreviewImageSource
        {
            public bool IsAvailable { get; set; }
            public DateTime ContentVersionUtc => new(2026, 1, 1);
            public int AppearanceCalls;

            public IconImage? Render(ResourceType type, string resRef, bool useIndexedBlueprint = false) =>
                Image();

            public IconImage? RenderModel(string modelResRef) => Image();

            public IconImage? RenderTileGroup(
                IReadOnlyList<string> slotModelResRefs, int columns, int rows) => Image();

            public IconImage? RenderCreatureAppearance(int appearanceId)
            {
                Interlocked.Increment(ref AppearanceCalls);
                return Image();
            }

            private static IconImage Image() => new(2, 2, new byte[2 * 2 * 4]);
        }

        private sealed class BindingLogSink : ILogSink
        {
            public List<string> Errors { get; } = [];

            public bool IsEnabled(LogEventLevel level, string area) =>
                level >= LogEventLevel.Warning && area == LogArea.Binding;

            public void Log(
                LogEventLevel level,
                string area,
                object? source,
                string messageTemplate) => Errors.Add(messageTemplate);

            public void Log(
                LogEventLevel level,
                string area,
                object? source,
                string messageTemplate,
                params object?[] propertyValues) => Errors.Add(
                $"{messageTemplate} :: {string.Join(", ", propertyValues.Select(value => value ?? "<null>"))}");
        }
    }
}
