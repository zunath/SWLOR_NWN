using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Placeables;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The picker option sets and preview caches every editor shares.
    /// </summary>
    /// <remarks>
    /// These were per-editor: opening a second placeable rebuilt the tag index projection, the
    /// visual-effect list, and every blueprint list from scratch, and held a second copy of each.
    /// </remarks>
    [TestFixture]
    public class SharedPickerCacheTests
    {
        [Test]
        public void OneSourceIsBuiltOnceAndHandedBackToEveryCaller()
        {
            var builds = 0;
            var provider = new BehaviorValueSourceProvider(
                gameCode: null,
                tags: () => null,
                blueprints: _ =>
                {
                    builds++;
                    return new[]
                    {
                        new CatalogEntry(ResourceType.Utc, "npc_guard", "Guard", "GUARD", "guard.utc.json")
                    };
                });

            var first = provider.GetOptions(PlaceableValueSource.CreatureBlueprints);
            var second = provider.GetOptions(PlaceableValueSource.CreatureBlueprints);

            builds.Should().Be(1);
            second.Should().BeSameAs(first);
        }

        [Test]
        public void ModuleBackedSourcesAreDroppedWhenTheModuleChanges()
        {
            var resRef = "npc_guard";
            var provider = new BehaviorValueSourceProvider(
                gameCode: null,
                tags: () => null,
                blueprints: _ => new[]
                {
                    new CatalogEntry(ResourceType.Utc, resRef, null, null, resRef + ".utc.json")
                });

            provider.GetOptions(PlaceableValueSource.CreatureBlueprints)
                .Should().ContainSingle().Which.Value.Should().Be("npc_guard");

            resRef = "npc_sentry";
            provider.InvalidateModuleSources();

            provider.GetOptions(PlaceableValueSource.CreatureBlueprints)
                .Should().ContainSingle().Which.Value.Should().Be("npc_sentry");
        }

        [Test]
        public void AnUnknownValueIsNeverReportedWrongWhenTheIndexDidNotLoad()
        {
            var provider = new BehaviorValueSourceProvider(gameCode: null, tags: () => null);

            provider.IsKnown(PlaceableValueSource.ObjectTags, "SOMETHING").Should().BeTrue();
            provider.IsKnown(PlaceableValueSource.ObjectTags, null).Should().BeTrue();
        }

        [Test]
        public void VisualEffectPreviewsAreDecodedAtTheSizeTheGalleryDrawsThem()
        {
            // 524 effects are named in the reference sheet. Holding those at published resolution
            // to fill a 158-pixel tile is the difference between tens and hundreds of megabytes.
            VfxPreviewService.PreviewWidth.Should().BeLessThan(512);
            VfxPreviewService.PreviewWidth.Should().BeGreaterThan(128);
        }

        [Test]
        public void AVisualEffectPreviewIsNullUntilSomethingHasFetchedIt()
        {
            var service = new VfxPreviewService();

            service.Cached(null).Should().BeNull();
            service.Cached("   ").Should().BeNull();
            service.Cached("https://example.invalid/never-fetched.png").Should().BeNull();
        }
    }
}
