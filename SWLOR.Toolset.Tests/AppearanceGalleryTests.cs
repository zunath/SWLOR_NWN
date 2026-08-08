using Avalonia.Headless.NUnit;
using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
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
            section.Tiles[0].HasPreview.Should().BeFalse();
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
        public void DoorAppearancePreviewRequestsTransitionFallbackRendering()
        {
            var source = new AppearancePreviewSource { IsAvailable = true };
            var thumbnails = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);

            thumbnails.RequestTileAsync("tn_gdoor_08", _ => { });
            DrainDispatcher();
            var ordinary = thumbnails.CachedTile("tn_gdoor_08");

            thumbnails.RequestTileAsync(
                "tn_gdoor_08",
                _ => { },
                renderDoorTransitionFallback: true);
            DrainDispatcher();
            var transition = thumbnails.CachedTile(
                "tn_gdoor_08",
                renderDoorTransitionFallback: true);

            ordinary.Should().NotBeNull();
            transition.Should().NotBeNull().And.NotBeSameAs(ordinary);

            using var section = new AppearanceGallerySectionViewModel(
                [
                    new AppearanceOption(
                        "transition",
                        "Transition",
                        "tn_gdoor_08",
                        ModelResRef: "tn_gdoor_08",
                        IsDoorTransition: true)
                ],
                thumbnails,
                () => "transition",
                _ => true,
                noun: "appearance");

            var tile = section.Tiles.Single();
            tile.Preview.Should().BeSameAs(transition,
                "the rebuilt gallery must read the transition-aware cache entry");
            tile.PreviewRequested.Should().BeTrue();

            section.EnsurePreview(tile);
            DrainDispatcher();

            source.ModelTransitionRequests.Should().Equal(new[] { false, true },
                "the cached transition preview should avoid a third render request");
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

            DrainDispatcher();
            source.AppearanceCalls.Should().Be(1,
                "the selected appearance is prefetched, but unselected off-screen cells must wait");

            section.EnsurePreview(section.Tiles[0]);
            section.EnsurePreview(section.Tiles[1]);
            section.EnsurePreview(section.Tiles[0]);
            DrainDispatcher();

            source.AppearanceCalls.Should().Be(2,
                "the selected cell and one newly realized cell render without duplicate work");
            section.Tiles.Take(2).Should().OnlyContain(tile => tile.Preview != null);
            section.Tiles.Skip(2).Should().OnlyContain(tile => tile.Preview == null);
        }

        [AvaloniaTest]
        public void TheRenderedGalleryRequestsPreviewsForItsVisibleCellsOnly()
        {
            var source = new AppearancePreviewSource { IsAvailable = true };
            var thumbnails = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);
            using var section = new AppearanceGallerySectionViewModel(
                Options(500), thumbnails, () => "0", _ => true, noun: "appearance");
            var view = new AppearanceGalleryView { DataContext = section };
            var window = new Window { Width = 760, Height = 560, Content = view };

            try
            {
                window.Show();
                DrainDispatcher();

                source.AppearanceCalls.Should().BeGreaterThan(0,
                    "realized gallery cells must request their model previews");
                source.AppearanceCalls.Should().BeLessThanOrEqualTo(48,
                    "opening the tab should render only the virtualized first page");
                section.Tiles.Should().Contain(tile => tile.Preview != null,
                    "a request count is not sufficient: a visible cell must receive the rendered bitmap");
                section.Tiles.Should().HaveCount(48,
                    "initial layout is not a user scroll and must not publish a second page");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void ADeferredCatalogDeliversPreviewsAfterTheGalleryIsAlreadyAttached()
        {
            var source = new AppearancePreviewSource { IsAvailable = true };
            var thumbnails = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);
            using var section = new AppearanceGallerySectionViewModel(
                Array.Empty<AppearanceOption>(), thumbnails, () => "0", _ => true, noun: "appearance");
            var window = new Window
            {
                Width = 760,
                Height = 560,
                Content = new AppearanceGalleryView { DataContext = section }
            };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                section.SetOptions(Options(500));
                DrainDispatcher();

                source.AppearanceCalls.Should().BeGreaterThan(0,
                    "the creature catalog arrives after the Appearance view is already attached");
                section.Tiles.Should().Contain(tile => tile.Preview != null,
                    "realized cells created by the deferred catalog must receive their bitmaps");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest, NonParallelizable]
        public void TheRealCreatureGalleryDeliversAHakBackedPreviewToAVisibleTile()
        {
            var repositoryRoot = CorpusLocator.RepositoryRoot;
            var haksRoot = Path.Combine(repositoryRoot, "SWLOR_Haks");
            if (!Directory.Exists(Path.Combine(haksRoot, "sw_2da")))
                haksRoot = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT") ?? haksRoot;
            if (!Directory.Exists(Path.Combine(haksRoot, "sw_2da")))
            {
                Assert.Ignore(
                    "The SWLOR_Haks submodule is not initialized and SWLOR_TEST_HAKS_ROOT was not supplied.");
            }

            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
                Assert.Ignore("No local NWN:EE installation was found for appearance models.");

            var twoDa = new TwoDaService(Path.Combine(haksRoot, "sw_2da"));
            var tlk = TlkService.Load(Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json"));
            var appearances = new AppearanceService(twoDa, tlk);
            var resources = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(repositoryRoot, "Build", "hakbuilder.json"),
                haksRoot,
                KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
            resources.EnsureInitialized();

            var context = new WorkspaceContext(
                path => new ModuleWorkspace(path, resources),
                new OutputLogService());
            context.Open(Path.Combine(repositoryRoot, "Module"));
            var renderer = new BlueprintPreviewRenderer(
                context,
                resources,
                appearances: appearances,
                twoDa: twoDa,
                tlk: tlk);
            var thumbnails = new ThumbnailService(context, renderer);
            using var section = new AppearanceGallerySectionViewModel(
                Array.Empty<AppearanceOption>(), thumbnails, () => "7", _ => true, noun: "appearance");
            var window = new Window
            {
                Width = 760,
                Height = 560,
                Content = new AppearanceGalleryView { DataContext = section }
            };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                section.SetOptions(appearances.GetAll()
                    .Take(96)
                    .Select(row => new AppearanceOption(
                        row.Id.ToString(), row.DisplayName, $"row {row.Id} · {row.Label}",
                        CreatureAppearanceId: row.Id,
                        IsSegmentedCreatureAppearance:
                            string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase)))
                    .ToList());

                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (DateTime.UtcNow < deadline && section.Tiles.All(tile => tile.Preview == null))
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(25);
                }
                Dispatcher.UIThread.RunJobs();

                section.Tiles.Should().Contain(tile => tile.Preview != null,
                    "the production renderer must deliver a bitmap to a realized gallery cell");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest, NonParallelizable]
        public void TheStartupHakStackDeliversAVisibleCreatureAppearancePreview()
        {
            var repositoryRoot = CorpusLocator.RepositoryRoot;
            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
                Assert.Ignore("No local NWN:EE installation was found for appearance models.");

            var profile = NwnIniProfile.Load();
            var ifoPath = Path.Combine(repositoryRoot, "Module", "ifo", "module.ifo.json");
            if (profile.HakDirectory == null || !File.Exists(ifoPath))
                Assert.Ignore("The local NWN profile does not expose the module's packed HAK stack.");

            var resolution = profile.ResolveHakLayers(IfoDocument.Load(ifoPath).HakNames);
            if (resolution.MissingHakNames.Count > 0)
                Assert.Ignore("The local NWN profile is missing one or more module HAKs.");

            var resources = ResourceIndex.CreateDeferred(
                resolution.Layers,
                () => KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
            var twoDa = new TwoDaService(resources);
            var haksRoot = Path.Combine(repositoryRoot, "SWLOR_Haks");
            if (!Directory.Exists(Path.Combine(haksRoot, "sw_tlk")))
                haksRoot = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT") ?? haksRoot;
            if (!File.Exists(Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json")))
            {
                Assert.Ignore(
                    "The SWLOR_Haks submodule is not initialized and SWLOR_TEST_HAKS_ROOT was not supplied.");
            }
            var tlk = TlkService.Load(Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json"));
            var appearances = new AppearanceService(twoDa, tlk);

            var context = new WorkspaceContext(
                path => new ModuleWorkspace(path, resources),
                new OutputLogService());
            var coldModuleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-appearance-cold-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(coldModuleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(coldModuleRoot, "utc"));
            context.Open(coldModuleRoot);
            var thumbnails = new ThumbnailService(
                context,
                new BlueprintPreviewRenderer(
                    context,
                    resources,
                    appearances: appearances,
                    twoDa: twoDa,
                    tlk: tlk));
            using var section = new AppearanceGallerySectionViewModel(
                Array.Empty<AppearanceOption>(), thumbnails, () => "2039", _ => true, noun: "appearance");
            var window = new Window
            {
                Width = 760,
                Height = 560,
                Content = new AppearanceGalleryView { DataContext = section }
            };

            try
            {
                thumbnails.WarmGenericSegmentedCreaturePreviews();
                window.Show();
                Dispatcher.UIThread.RunJobs();
                section.SetOptions(appearances.GetAll()
                    .Take(96)
                    .Select(row => new AppearanceOption(
                        row.Id.ToString(), row.DisplayName, $"row {row.Id} - {row.Label}",
                        CreatureAppearanceId: row.Id,
                        IsSegmentedCreatureAppearance:
                            string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase)))
                    .ToList());

                var dynamicTiles = section.Tiles
                    .Where(tile => tile.Option.IsSegmentedCreatureAppearance)
                    .Take(7)
                    .ToList();
                dynamicTiles.Should().HaveCount(7,
                    "the cold-gallery regression covers Dwarf through Human");
                var currentTile = section.Tiles.Single(tile => tile.Option.Key == "7");
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (DateTime.UtcNow < deadline &&
                       (currentTile.Preview == null || dynamicTiles.Any(tile => tile.Preview == null)))
                {
                    Dispatcher.UIThread.RunJobs();
                    Thread.Sleep(25);
                }
                Dispatcher.UIThread.RunJobs();

                currentTile.Preview.Should().NotBeNull(
                    "an ordinary appearance must load while dynamic creatures render");
                dynamicTiles.Should().OnlyContain(tile => tile.Preview != null,
                    "all seven unselected base dynamic creatures must show representative models on a cold gallery");
            }
            finally
            {
                window.Close();
                thumbnails.ClearCache();
                Directory.Delete(coldModuleRoot, recursive: true);
            }
        }

        [Test]
        public void ReplacingTheCatalogPublishesItsFirstPageAsOneCollectionChange()
        {
            var section = Section(Array.Empty<AppearanceOption>(), out _);
            var tilePropertyChanges = 0;
            section.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(section.Tiles))
                    tilePropertyChanges++;
            };

            section.SetOptions(Options(2_000));

            tilePropertyChanges.Should().Be(1,
                "catalog loading must not make Avalonia lay out the tab once per published tile");
            section.Tiles.Should().HaveCount(48);
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
            appearanceView.Should().Contain("IsVisible=\"{Binding !HasPreview}\"",
                "the letter is only a temporary placeholder and must not remain behind real artwork");
            appearanceView.Should().Contain("IsVisible=\"{Binding HasPreview}\"",
                "the rendered model replaces rather than overlays the fallback letter");

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
            public ConcurrentQueue<bool> ModelTransitionRequests { get; } = new();

            public IconImage? Render(ResourceType type, string resRef, bool useIndexedBlueprint = false) =>
                Image();

            public IconImage? RenderModel(string modelResRef, bool renderDoorTransitionFallback = false)
            {
                ModelTransitionRequests.Enqueue(renderDoorTransitionFallback);
                return Image();
            }

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
