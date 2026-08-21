using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The catalog's keyed lookups, and the ordered snapshot they sit beside.
    /// </summary>
    /// <remarks>
    /// Both exist for the same reason: the production corpus is ~17,900 records, and the callers
    /// that only wanted one name were walking all of them - the area editor's selection bar on every
    /// click, and the placeable pickers once per requested type.
    /// </remarks>
    [TestFixture]
    public class BlueprintCatalogLookupTests
    {
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(Path.GetTempPath(), "swlor-catalog-" + Guid.NewGuid().ToString("N"));
            // ModuleWorkspace insists on the folders a real module has before it will open one.
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "utc"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "utp"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "uti"));

            WriteCreature("npc_guard", "Guard", "GUARD_TAG");
            WriteCreature("npc_sentry", "Sentry", "SENTRY_TAG");
            WritePlaceable("crate_small", "Small Crate", "CRATE");
            WriteItem("probe_item", "Probe Item", "PROBE_ITEM", 75);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(_moduleRoot, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp directory is not a test failure.
            }
        }

        [Test]
        public async Task OneEntryIsFoundByTypeAndResRefWithoutWalkingTheSnapshot()
        {
            var catalog = await BuiltCatalogAsync();

            catalog.TryGetEntry(ResourceType.Utc, "npc_guard", out var guard).Should().BeTrue();
            guard.Name.Should().Be("Guard");
            guard.Tag.Should().Be("GUARD_TAG");

            // The dictionary is case-insensitive, exactly as the snapshot scan was.
            catalog.TryGetEntry(ResourceType.Utc, "NPC_GUARD", out _).Should().BeTrue();

            catalog.TryGetEntry(ResourceType.Utp, "npc_guard", out _)
                .Should().BeFalse("a resref means nothing without its type");
            catalog.TryGetEntry(ResourceType.Utc, "missing", out _).Should().BeFalse();
            catalog.TryGetEntry(ResourceType.Utc, "  ", out _).Should().BeFalse();
        }

        [Test]
        public async Task EntriesOfTypeReturnsOnlyThatType()
        {
            var catalog = await BuiltCatalogAsync();

            catalog.EntriesOfType(ResourceType.Utc)
                .Select(entry => entry.ResRef)
                .Should().BeEquivalentTo("npc_guard", "npc_sentry");
            catalog.EntriesOfType(ResourceType.Utp)
                .Select(entry => entry.ResRef)
                .Should().BeEquivalentTo("crate_small");
        }

        [Test]
        public async Task ItemBaseTypeIsRetainedFromTheCatalogParse()
        {
            var catalog = await BuiltCatalogAsync();

            catalog.TryGetEntry(ResourceType.Uti, "probe_item", out var item).Should().BeTrue();
            item.Name.Should().Be("Probe Item");
            item.BaseItem.Should().Be(75,
                "merchant category searches should reuse catalog metadata instead of parsing the item again");
        }

        [Test]
        public async Task TheSnapshotStaysOrderedAndPicksUpRefreshesAndRemovals()
        {
            var catalog = await BuiltCatalogAsync();

            catalog.Entries.Select(entry => entry.ResRef)
                .Should().ContainInOrder("npc_guard", "npc_sentry");

            WriteCreature("npc_guard", "Renamed Guard", "GUARD_TAG");
            catalog.RefreshEntry(ResourceType.Utc, "npc_guard");
            catalog.Entries.Single(entry => entry.ResRef == "npc_guard").Name
                .Should().Be("Renamed Guard");

            catalog.RemoveEntry(ResourceType.Utc, "npc_sentry");
            catalog.Entries.Should().NotContain(entry => entry.ResRef == "npc_sentry");
            catalog.TryGetEntry(ResourceType.Utc, "npc_sentry", out _).Should().BeFalse();
        }

        [Test]
        public async Task RefreshWithUnchangedMetadataKeepsThePublishedSnapshot()
        {
            var catalog = await BuiltCatalogAsync();
            var published = catalog.Entries;

            var refreshed = catalog.RefreshEntry(
                ResourceType.Utc,
                "npc_guard",
                out var changed);

            refreshed.Should().NotBeNull();
            changed.Should().BeFalse();
            catalog.Entries.Should().BeSameAs(published,
                "content-only saves must not re-sort and republish the full catalog");
        }

        [Test]
        public async Task RefreshRemovesAResourceThatDisappearedBeforeItCouldBeRead()
        {
            var catalog = await BuiltCatalogAsync();
            var path = Path.Combine(_moduleRoot, "utc", "npc_guard.utc.json");
            File.Delete(path);

            catalog.RefreshEntry(ResourceType.Utc, "npc_guard").Should().BeNull();

            catalog.TryGetEntry(ResourceType.Utc, "npc_guard", out _).Should().BeFalse();
            catalog.Entries.Should().NotContain(entry => entry.ResRef == "npc_guard");
        }

        [Test]
        public async Task SearchRanksExactThenPrefixThenContainsAndStopsAtTheLimit()
        {
            var catalog = await BuiltCatalogAsync();

            var ranked = catalog.Search("npc");
            ranked.Should().HaveCount(2);
            ranked.Should().OnlyContain(result => result.MatchKind == CatalogMatchKind.Prefix);

            catalog.Search("npc_guard")[0].MatchKind.Should().Be(CatalogMatchKind.ExactResRef);
            catalog.Search("guard")[0].MatchKind.Should().Be(CatalogMatchKind.Prefix,
                "the tag GUARD_TAG starts with it; nothing has it as a whole ResRef");
            catalog.Search("uar")[0].MatchKind.Should().Be(CatalogMatchKind.Contains);

            // A better-ranked hit is never displaced by the limit.
            var limited = catalog.Search("npc", limit: 1);
            limited.Should().ContainSingle();
            limited[0].Entry.ResRef.Should().Be("npc_guard");

            catalog.Search("npc", limit: 0).Should().BeEmpty();
            catalog.Search("   ").Should().BeEmpty();
        }

        [Test]
        public async Task SearchNeverReturnsMoreThanTheLimitEvenWhenEverythingMatches()
        {
            var catalog = await BuiltCatalogAsync();

            // "_" appears in every seeded resref, so this is the shape a one-letter query takes on
            // the real corpus: most of the index matches and only the first screen is read.
            catalog.Search("_", limit: 2).Should().HaveCount(2);
        }

        private async Task<BlueprintCatalog> BuiltCatalogAsync()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);
            var catalog = new BlueprintCatalog(workspace);
            await catalog.BuildTask;
            return catalog;
        }

        private void WriteCreature(string resRef, string name, string tag) =>
            File.WriteAllBytes(
                Path.Combine(_moduleRoot, "utc", resRef + ".utc.json"),
                Encoding.UTF8.GetBytes($$"""
                {
                  "__data_type": "UTC ",
                  "FirstName": { "type": "cexolocstring", "value": { "0": "{{name}}" } },
                  "Tag": { "type": "cexostring", "value": "{{tag}}" }
                }
                """));

        private void WritePlaceable(string resRef, string name, string tag) =>
            File.WriteAllBytes(
                Path.Combine(_moduleRoot, "utp", resRef + ".utp.json"),
                Encoding.UTF8.GetBytes($$"""
                {
                  "__data_type": "UTP ",
                  "LocName": { "type": "cexolocstring", "value": { "0": "{{name}}" } },
                  "Tag": { "type": "cexostring", "value": "{{tag}}" }
                }
                """));

        private void WriteItem(string resRef, string name, string tag, int baseItem) =>
            File.WriteAllBytes(
                Path.Combine(_moduleRoot, "uti", resRef + ".uti.json"),
                Encoding.UTF8.GetBytes($$"""
                {
                  "__data_type": "UTI ",
                  "LocalizedName": { "type": "cexolocstring", "value": { "0": "{{name}}" } },
                  "Tag": { "type": "cexostring", "value": "{{tag}}" },
                  "BaseItem": { "type": "int", "value": {{baseItem}} }
                }
                """));
    }
}
