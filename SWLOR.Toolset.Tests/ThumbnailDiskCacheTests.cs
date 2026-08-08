using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class ThumbnailDiskCacheTests
    {
        private string _moduleRoot = string.Empty;
        private ThumbnailDiskCache _cache = null!;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-thumbnail-cache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_moduleRoot);
            _cache = new ThumbnailDiskCache(_moduleRoot);
        }

        [TearDown]
        public void TearDown()
        {
            _cache.Clear();
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        [Test]
        public void StandardAndCustomSourcesUseIndependentEntriesForTheSameResRef()
        {
            const string resRef = "shared_resref";
            _cache.StoreNoArtwork(ResourceType.Utp, resRef, useIndexedBlueprint: false);

            _cache.TryLoad(
                    ResourceType.Utp, resRef, blueprintPath: null,
                    useIndexedBlueprint: false, out _)
                .Should().Be(ThumbnailDiskCache.Lookup.NoArtwork);
            _cache.TryLoad(
                    ResourceType.Utp, resRef, blueprintPath: null,
                    useIndexedBlueprint: true, out _)
                .Should().Be(ThumbnailDiskCache.Lookup.Miss);

            _cache.StoreNoArtwork(ResourceType.Utp, resRef, useIndexedBlueprint: true);
            _cache.Remove(ResourceType.Utp, resRef, useIndexedBlueprint: true);

            _cache.TryLoad(
                    ResourceType.Utp, resRef, blueprintPath: null,
                    useIndexedBlueprint: false, out _)
                .Should().Be(
                    ThumbnailDiskCache.Lookup.NoArtwork,
                    "invalidating a Standard preview must not erase the Custom entry");
        }

        [Test]
        public void CreatureCacheBecomesStaleWhenItsEquippedItemChanges()
        {
            var blueprintPath = Path.Combine(_moduleRoot, "creature.utc.json");
            var armorPath = Path.Combine(_moduleRoot, "armor.uti.json");
            File.WriteAllText(blueprintPath, "creature");
            File.WriteAllText(armorPath, "armor");
            _cache.StoreNoArtwork(ResourceType.Utc, "creature", useIndexedBlueprint: false);

            _cache.TryLoad(
                    ResourceType.Utc,
                    "creature",
                    blueprintPath,
                    useIndexedBlueprint: false,
                    out _,
                    new[] { armorPath })
                .Should().Be(ThumbnailDiskCache.Lookup.NoArtwork);

            File.SetLastWriteTimeUtc(armorPath, DateTime.UtcNow.AddMinutes(1));

            _cache.TryLoad(
                    ResourceType.Utc,
                    "creature",
                    blueprintPath,
                    useIndexedBlueprint: false,
                    out _,
                    new[] { armorPath })
                .Should().Be(
                    ThumbnailDiskCache.Lookup.Miss,
                    "the equipped UTI contributes visible armor to the UTC preview");
        }
    }
}
