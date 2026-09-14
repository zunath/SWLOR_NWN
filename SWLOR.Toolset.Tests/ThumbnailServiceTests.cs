using Avalonia.Headless.NUnit;
using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The preview cache's own logic: coalescing concurrent requests, answering from memory, and
    /// deciding when an in-flight render has been invalidated out from under itself.
    /// </summary>
    /// <remarks>
    /// This is the layer where the bugs live, and until <see cref="IPreviewImageSource"/> existed
    /// none of it could be exercised — reaching a real render needs a resolved NWN install, a hak
    /// stack, and seconds per image. The fake below counts calls and answers instantly, which is all
    /// the cache needs to be interrogated.
    /// </remarks>
    [NonParallelizable]
    public class ThumbnailServiceTests
    {
        [AvaloniaTest]
        public void AModelIsRenderedOnceAndAnsweredFromMemoryAfterThat()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            var first = Request(service, "fci01_b01_01");
            first.Should().NotBeNull();
            source.ModelCalls.Should().Be(1);

            service.CachedTile("fci01_b01_01").Should().NotBeNull();

            var second = Request(service, "fci01_b01_01");
            second.Should().BeSameAs(first, "the second request is answered from memory");
            source.ModelCalls.Should().Be(1, "nothing should be rendered twice");
        }

        [AvaloniaTest]
        public void DoorTransitionModelsUseTheirOwnFallbackAwareCacheEntry()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(
                new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                source);

            service.RequestTileAsync(
                "shared_model",
                _ => { },
                renderDoorTransitionFallback: true);
            Drain();

            service.CachedTile("shared_model", renderDoorTransitionFallback: true).Should().NotBeNull();
            service.CachedTile("shared_model").Should().BeNull(
                "an ordinary model render must not reuse a transition fallback thumbnail");

            service.RequestTileAsync("shared_model", _ => { });
            Drain();

            source.ModelCalls.Should().Be(2);
            source.ModelTransitionRequests.Should().Equal(true, false);
        }

        [AvaloniaTest]
        public void EveryCallerWaitingOnOneRenderIsCalledBack()
        {
            // Four tileset groups routinely share a preview model. An earlier version tracked only
            // "is running", so the second request saw the first in flight and returned without ever
            // being called back - three of the four cells stayed permanently blank.
            var source = new CountingSource { BlockUntilReleased = true };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            var delivered = 0;
            for (var caller = 0; caller < 4; caller++)
                service.RequestTileAsync("shared_model", _ => delivered++);

            source.Release();
            Drain();

            delivered.Should().Be(4, "every caller that asked has to be told");
            source.ModelCalls.Should().Be(1, "one render serves all four");
        }

        [AvaloniaTest]
        public void AKeyWithNoArtworkIsRememberedRatherThanRetriedForever()
        {
            var source = new CountingSource { ModelResult = null };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.RequestTileAsync("nothing_here", _ => { });
            Drain();
            service.RequestTileAsync("nothing_here", _ => { });
            Drain();

            source.ModelCalls.Should().Be(1, "an answered \"no artwork\" is still an answer");
        }

        [AvaloniaTest]
        public void ACachedGroupLookupMustBeAskedWithItsOwnFootprintOrItMissesTheGroupsRender()
        {
            // A multi-slot group renders and caches under a composite key of its whole footprint
            // (RequestTileAsync), not under its first model's plain key - two groups can share a
            // first tile and still look nothing alike. A lookup that omits the footprint therefore
            // must not return the group's render; it can only ever answer for that one model on its
            // own, which is a different cache entry.
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);
            var footprint = new[] { "shared_a", "shared_b" };

            service.RequestTileAsync("shared_a", _ => { });
            Drain();
            var single = service.CachedTile("shared_a");
            single.Should().NotBeNull();

            service.RequestTileAsync("shared_a", _ => { }, footprint, columns: 2, rows: 1);
            Drain();
            source.ModelCalls.Should().Be(2, "the group is a second, distinct render - not answered from " +
                "the single tile's cache entry");

            service.CachedTile("shared_a", footprint, columns: 2, rows: 1)
                .Should().NotBeSameAs(single, "the group's own cache entry, not the single tile's");
            service.CachedTile("shared_a")
                .Should().BeSameAs(single, "a footprint-less lookup must still answer only for the plain key");
        }

        [AvaloniaTest]
        public void AnAppearanceRowIsCachedUnderItsOwnKeyRatherThanColliding()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.CachedAppearance(7).Should().BeNull();

            service.RequestAppearanceAsync(7, _ => { });
            Drain();

            service.CachedAppearance(7).Should().NotBeNull();
            source.AppearanceCalls.Should().Be(1);

            // A row id must not be answered by a model or blueprint of the same name.
            service.CachedTile("7").Should().BeNull();
            service.Cached(ResourceType.Utc, "7").Should().BeNull();
        }

        [AvaloniaTest]
        public void GenericSegmentedCreaturePreviewsAreRequeuedAfterACacheClear()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.WarmGenericSegmentedCreaturePreviews();
            Drain();

            source.AppearanceOrder.Should().BeEquivalentTo(
                new[] { 0, 1, 2, 3, 4, 5, 6 },
                "every stock dynamic race needs a representative model");
            Enumerable.Range(0, 7).Should().OnlyContain(id => service.CachedAppearance(id) != null);

            service.ClearCache();
            service.WarmGenericSegmentedCreaturePreviews();
            Drain();

            source.AppearanceCalls.Should().Be(14,
                "a HAK reload invalidates the old pixels and all seven representatives must render again");
            Enumerable.Range(0, 7).Should().OnlyContain(id => service.CachedAppearance(id) != null);
        }

        [AvaloniaTest]
        public void AnAppearanceMissIsRetriedWhenResourcesBecomeAvailable()
        {
            // The editor can publish its first gallery page while the replacement module HAK stack
            // is still loading. That miss is temporary and must not become a permanent null cache
            // entry that defeats ReloadPreviews after ResourcesReloaded.
            var source = new CountingSource { AppearanceResult = null };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.RequestAppearanceAsync(7, _ => { });
            Drain();

            service.CachedAppearance(7).Should().BeNull();
            source.AppearanceCalls.Should().Be(1);

            source.AppearanceResult = CountingSource.Image();
            service.RequestAppearanceAsync(7, _ => { });
            Drain();

            source.AppearanceCalls.Should().Be(2,
                "a failed early render must remain retryable after game resources reload");
            service.CachedAppearance(7).Should().NotBeNull();
        }

        [AvaloniaTest]
        public void ClearingTheCacheMakesTheNextRequestRenderAgain()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            Request(service, "a_model");
            source.ModelCalls.Should().Be(1);

            service.ClearCache();

            Request(service, "a_model");
            source.ModelCalls.Should().Be(2, "a cleared cache has nothing to answer from");
        }

        [AvaloniaTest]
        public async Task EditingACloakInvalidatesEveryCreatureThumbnailThatWearsIt()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-thumbnail-equipment-" + Guid.NewGuid().ToString("N"));
            try
            {
                var utcDirectory = Path.Combine(moduleRoot, "utc");
                Directory.CreateDirectory(utcDirectory);
                Directory.CreateDirectory(Path.Combine(moduleRoot, "are"));
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, "utc", "darthgravius.utc.json"),
                    Path.Combine(utcDirectory, "darthgravius.utc.json"));

                var context = new WorkspaceContext(
                    path => new ModuleWorkspace(path), new OutputLogService());
                context.Open(moduleRoot);
                await context.Catalog!.BuildTask;
                var service = new ThumbnailService(context, new CountingSource());
                var invalidated = new List<(ResourceType Type, string ResRef)>();
                service.InvalidatedForResRef += (type, resRef) => invalidated.Add((type, resRef));

                service.RequestAsync(ResourceType.Utc, "darthgravius", _ => { });
                Drain();
                service.Cached(ResourceType.Utc, "darthgravius").Should().NotBeNull();

                service.Invalidate(ResourceType.Uti, "jeweled_cloak");

                service.Cached(ResourceType.Utc, "darthgravius").Should().BeNull();
                invalidated.Should().Contain((ResourceType.Utc, "darthgravius"),
                    "cloak, helmet, and held-item edits must invalidate wearers just like chest armor");
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        [Test]
        public async Task CreatureDiskDependenciesIncludeOnlyLooseEquippedItems()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-thumbnail-dependencies-" + Guid.NewGuid().ToString("N"));
            try
            {
                var utcDirectory = Path.Combine(moduleRoot, "utc");
                var utiDirectory = Path.Combine(moduleRoot, "uti");
                Directory.CreateDirectory(utcDirectory);
                Directory.CreateDirectory(utiDirectory);
                Directory.CreateDirectory(Path.Combine(moduleRoot, "are"));
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, "utc", "darthgravius.utc.json"),
                    Path.Combine(utcDirectory, "darthgravius.utc.json"));
                var looseCloakPath = Path.Combine(utiDirectory, "jeweled_cloak.uti.json");
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, "uti", "jeweled_cloak.uti.json"),
                    looseCloakPath);

                var context = new WorkspaceContext(
                    path => new ModuleWorkspace(path), new OutputLogService());
                context.Open(moduleRoot);
                await context.Catalog!.BuildTask;
                var service = new ThumbnailService(context, new CountingSource());
                var method = typeof(ThumbnailService).GetMethod(
                    "DependencyPaths",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

                var paths = (IReadOnlyList<string>)method.Invoke(
                    service,
                    new object[] { ResourceType.Utc, "darthgravius", false })!;

                paths.Should().ContainSingle()
                    .Which.Should().Be(
                        looseCloakPath,
                        "indexed or HAK equipment has no loose timestamp and is covered by the content version");
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        [AvaloniaTest]
        public void ARenderInvalidatedWhileItWasRunningIsNotPublished()
        {
            // The whole point of the epoch: a render that started before the cache was cleared is
            // answering a question about game data that has since changed.
            var source = new CountingSource { BlockUntilReleased = true };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.RequestTileAsync("stale_model", _ => { });
            service.ClearCache();
            source.Release();
            Drain();

            service.CachedTile("stale_model").Should().BeNull(
                "the result belongs to an epoch that no longer exists");
        }

        [AvaloniaTest]
        public void AnInvalidatedAppearanceRenderReleasesItsWaitingTileForRetry()
        {
            var source = new CountingSource { BlockUntilReleased = true };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);
            var delivered = 0;
            var failed = 0;

            service.RequestAppearanceAsync(3, _ => delivered++, () => failed++);
            service.ClearCache();
            source.Release();
            Drain();

            delivered.Should().Be(0, "stale appearance pixels must not be published");
            failed.Should().Be(1,
                "the realized tile must be told to clear its requested state and retry the new epoch");
        }

        [AvaloniaTest]
        public void VisibleAppearanceRequestsOvertakeDeferredSegmentedRows()
        {
            var source = new CountingSource { BlockUntilReleased = true };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            // Occupy both bounded workers as the first segmented rows in a gallery would. The
            // remaining queue then has to prefer a newly realized simple row over more expensive
            // off-screen segmented rows.
            service.RequestAppearanceAsync(
                0, _ => { }, priority: AppearancePreviewPriority.Deferred);
            service.RequestAppearanceAsync(
                1, _ => { }, priority: AppearancePreviewPriority.Deferred);
            SpinWait.SpinUntil(() => source.AppearanceOrder.Count >= 2, TimeSpan.FromSeconds(2))
                .Should().BeTrue();

            service.RequestAppearanceAsync(
                2, _ => { }, priority: AppearancePreviewPriority.Deferred);
            service.RequestAppearanceAsync(
                3, _ => { }, priority: AppearancePreviewPriority.Deferred);
            service.RequestAppearanceAsync(
                4, _ => { }, priority: AppearancePreviewPriority.Deferred);
            service.RequestAppearanceAsync(
                5, _ => { }, priority: AppearancePreviewPriority.Deferred);
            service.RequestAppearanceAsync(
                7, _ => { }, priority: AppearancePreviewPriority.Visible);

            source.Release();
            Drain();

            var order = source.AppearanceOrder.ToArray();
            order.Should().Contain(new[] { 2, 3, 4, 5, 7 });
            Array.IndexOf(order, 7).Should().BeLessThanOrEqualTo(3,
                "the two workers must start the visible preview before the deferred backlog");
        }

        [AvaloniaTest]
        public void AnUnavailableRendererMakesTheWholeCacheANoOp()
        {
            // What happens with no resolved repository layout: the palette falls back to letter
            // glyphs rather than sitting on an empty grid waiting for renders that cannot happen.
            var source = new CountingSource { IsAvailable = false };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.IsAvailable.Should().BeFalse();

            var delivered = 0;
            service.RequestTileAsync("anything", _ => delivered++);
            service.RequestAppearanceAsync(3, _ => delivered++);
            service.RequestAsync(ResourceType.Utc, "npc_guard", _ => delivered++);
            Drain();

            delivered.Should().Be(0);
            source.ModelCalls.Should().Be(0);
            service.Cached(ResourceType.Utc, "npc_guard").Should().BeNull();
        }

        [AvaloniaTest]
        public void ABlankRequestIsIgnoredRatherThanCached()
        {
            var source = new CountingSource();
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            service.RequestTileAsync("   ", _ => { });
            service.RequestAppearanceAsync(-1, _ => { });
            Drain();

            source.ModelCalls.Should().Be(0);
            source.AppearanceCalls.Should().Be(0);
        }

        [AvaloniaTest]
        public void ATypeSymbolIsDrawnOnceAndSharedByEveryTileThatNeedsIt()
        {
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), new CountingSource());

            var first = service.TypeIcon(ResourceType.Utc);
            service.TypeIcon(ResourceType.Utc).Should().BeSameAs(first,
                "thousands of tiles can want the same symbol and they are all identical");

            service.TypeIcon(ResourceType.Utp).Should().NotBeSameAs(first);
            service.TypeChipIcon(ResourceType.Utc).Should().NotBeSameAs(
                first, "the row chip is drawn at its own size rather than scaled down");
        }

        [AvaloniaTest]
        public void ARendererThatThrowsLeavesTheRestOfTheGridFillingIn()
        {
            var source = new CountingSource { ThrowOnModel = true };
            var service = new ThumbnailService(new WorkspaceContext(_ => throw new NotSupportedException(),
                new OutputLogService()), source);

            var act = () =>
            {
                service.RequestTileAsync("explodes", _ => { });
                Drain();
            };

            act.Should().NotThrow("one bad model must not take the palette down");
        }

        private static Avalonia.Media.Imaging.Bitmap? Request(ThumbnailService service, string model)
        {
            service.RequestTileAsync(model, _ => { });
            Drain();
            return service.CachedTile(model);
        }

        /// <summary>
        /// Lets the render task finish and its UI-thread callback run. The service deliberately does
        /// its work on the pool and publishes through the dispatcher, so a test has to do both.
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

        private sealed class CountingSource : IPreviewImageSource
        {
            private readonly ManualResetEventSlim _gate = new(initialState: true);

            public bool IsAvailable { get; init; } = true;

            public DateTime ContentVersionUtc => new(2026, 1, 1);

            public bool ThrowOnModel { get; init; }

            public IconImage? ModelResult { get; init; } = Image();

            public bool BlockUntilReleased
            {
                init
                {
                    if (value)
                        _gate.Reset();
                }
            }

            public int ModelCalls;
            public int AppearanceCalls;
            public ConcurrentQueue<int> AppearanceOrder { get; } = new();
            public ConcurrentQueue<bool> ModelTransitionRequests { get; } = new();

            public IconImage? AppearanceResult { get; set; } = Image();

            public void Release() => _gate.Set();

            public IconImage? Render(ResourceType type, string resRef, bool useIndexedBlueprint = false) =>
                Image();

            public IconImage? RenderTileGroup(IReadOnlyList<string> slotModelResRefs, int columns, int rows) =>
                RenderModel(slotModelResRefs.FirstOrDefault(slot => !string.IsNullOrWhiteSpace(slot)) ?? string.Empty);

            public IconImage? RenderModel(string modelResRef, bool renderDoorTransitionFallback = false)
            {
                _gate.Wait(TimeSpan.FromSeconds(5));
                Interlocked.Increment(ref ModelCalls);
                ModelTransitionRequests.Enqueue(renderDoorTransitionFallback);

                if (ThrowOnModel)
                    throw new InvalidOperationException("unparseable model");

                return ModelResult;
            }

            public IconImage? RenderCreatureAppearance(int appearanceId)
            {
                AppearanceOrder.Enqueue(appearanceId);
                _gate.Wait(TimeSpan.FromSeconds(5));
                Interlocked.Increment(ref AppearanceCalls);
                return AppearanceResult;
            }

            public static IconImage Image() => new(2, 2, new byte[2 * 2 * 4]);
        }
    }
}
