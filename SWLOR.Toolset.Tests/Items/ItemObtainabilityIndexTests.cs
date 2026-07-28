using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// <see cref="ItemObtainabilityIndex"/> and <see cref="ItemSourceSectionViewModel"/> coverage,
    /// built once over the real module + game-code corpus (<see cref="SharedIndex"/>) so every test
    /// in this fixture shares one scan instead of repeating the multi-thousand-file C# source pass.
    /// </summary>
    [TestFixture]
    public class ItemObtainabilityIndexTests
    {
        /// <summary>
        /// Locates the SWLOR.Game.Server source directory from the test execution context, walking
        /// up from the test assembly until a folder containing "SWLOR.Game.Server.csproj" is found.
        /// Deliberately independent from <see cref="CorpusLocator"/> and
        /// <c>GameCodeIndexTests.GameServerSourceRoot</c>'s identical-looking locator, per this
        /// repo's per-file locator convention.
        /// </summary>
        private static string GameServerSourceRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                    if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.csproj")))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the SWLOR.Game.Server source directory from the test context.");
            }
        }

        /// <summary>
        /// Built once for the whole fixture: the full corpus scan (module .utm stores plus every
        /// *.cs file under SWLOR.Game.Server) takes a few seconds, not the ~60s that would justify
        /// splitting it up further.
        /// </summary>
        private static readonly Lazy<ItemObtainabilityIndex> SharedIndex = new(() =>
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            return ItemObtainabilityIndex.Build(workspace, GameServerSourceRoot);
        });

        [Test]
        public void BuildsWithoutThrowingAndCoversTheModule()
        {
            var index = SharedIndex.Value;

            index.ItemsWithSources.Should().BeGreaterThan(1000);
        }

        [Test]
        public void StoreSoldItemNamesItsOwningStore()
        {
            var index = SharedIndex.Value;
            var (storeResRef, storeDisplay, itemResRef) = FirstStoreSoldItem();

            var sources = index.SourcesFor(itemResRef);

            sources.Should().Contain(entry =>
                entry.Kind == ItemSourceKind.Store &&
                entry.Display == storeDisplay &&
                entry.SourceResRef == storeResRef);
        }

        [Test]
        public void RecipeOutputResolvesToARecipeEntryNamedForTheRecipe()
        {
            var index = SharedIndex.Value;
            var (recipeName, itemResRef) = FirstRecipeOutput();

            var sources = index.SourcesFor(itemResRef);

            sources.Should().Contain(entry =>
                entry.Kind == ItemSourceKind.Recipe && entry.Display == recipeName);
        }

        [Test]
        public void NpcDroppableItemNamesItsCarryingCreature()
        {
            // equ_ferraille's only source is being carried droppable by ext_jawa003 - the exact
            // corpus case where a store-and-C#-only index wrongly reported "no player source".
            var sources = SharedIndex.Value.SourcesFor("equ_ferraille");

            sources.Should().Contain(entry =>
                entry.Kind == ItemSourceKind.Npc && entry.SourceResRef == "ext_jawa003");
        }

        [Test]
        public void PlacedContainerDefaultInventoryCountsAsASource()
        {
            var (containerResRef, itemResRef) = FirstContainerItem();

            var sources = SharedIndex.Value.SourcesFor(itemResRef);

            sources.Should().Contain(entry =>
                entry.Kind == ItemSourceKind.Container && entry.SourceResRef == containerResRef);
        }

        /// <summary>
        /// The first .utp in the corpus carrying a default ItemList entry, read independently of
        /// the index's own parser.
        /// </summary>
        private static (string ContainerResRef, string ItemResRef) FirstContainerItem()
        {
            foreach (var file in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "utp"), "*.utp.json"))
            {
                var text = File.ReadAllText(file);
                if (!text.Contains("InventoryRes"))
                    continue;

                using var doc = JsonDocument.Parse(text);
                if (!doc.RootElement.TryGetProperty("ItemList", out var wrapper) ||
                    !wrapper.TryGetProperty("value", out var items) ||
                    items.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("InventoryRes", out var res) &&
                        res.TryGetProperty("value", out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        var resRef = Path.GetFileName(file).Replace(".utp.json", string.Empty);
                        return (resRef, value.GetString()!);
                    }
                }
            }

            throw new InvalidOperationException("No .utp with a default inventory found in the corpus.");
        }

        [Test]
        public void UnknownResRefHasNoSourcesAndIsNotObtainable()
        {
            var index = SharedIndex.Value;

            index.SourcesFor("zzz_not_an_item").Should().BeEmpty();
            index.IsObtainable("zzz_not_an_item").Should().BeFalse();
        }

        [Test]
        public void SectionViewModel_ReportsAnObtainableVerdictWithOrderedGroups()
        {
            var index = SharedIndex.Value;
            var (_, _, obtainableResRef) = FirstStoreSoldItem();

            var section = new ItemSourceSectionViewModel(obtainableResRef, index.SourcesFor);

            section.IsLoaded.Should().BeTrue();
            section.IsObtainable.Should().BeTrue();
            section.Verdict.Should().Be(
                $"✓ Obtainable — {index.SourcesFor(obtainableResRef).Count} sources in the module");

            var expectedOrder = new[] { "Store", "Recipe", "Loot", "Quest", "Npc", "Container", "Other" };
            var groupTitles = section.Groups.Select(group => group.Title).ToList();
            groupTitles.Select(title => Array.IndexOf(expectedOrder, title))
                .Should().BeInAscendingOrder();

            groupTitles.Should().Contain("Store");
            foreach (var title in groupTitles)
                section.EmptyKinds.Should().NotContain(title);

            (groupTitles.Count + section.EmptyKinds.Count).Should().Be(expectedOrder.Length);
        }

        [Test]
        public void SectionViewModel_ReportsANoSourceVerdictWithEveryKindEmpty()
        {
            var index = SharedIndex.Value;

            var section = new ItemSourceSectionViewModel("zzz_not_an_item", index.SourcesFor);

            section.IsObtainable.Should().BeFalse();
            section.Verdict.Should().Be("No player source grants this item");
            section.Groups.Should().BeEmpty();
            section.EmptyKinds.Should().HaveCount(7);
        }

        [Test]
        public void SectionViewModel_WithoutALookupReportsNotLoaded()
        {
            var section = new ItemSourceSectionViewModel("anything", null);

            section.IsLoaded.Should().BeFalse();
            section.IsObtainable.Should().BeFalse();
        }

        [Test]
        public void SectionViewModel_RefreshRequeriesForTheNewResRef()
        {
            var index = SharedIndex.Value;
            var (_, _, obtainableResRef) = FirstStoreSoldItem();

            var section = new ItemSourceSectionViewModel("zzz_not_an_item", index.SourcesFor);
            section.IsObtainable.Should().BeFalse();

            section.Refresh(obtainableResRef);

            section.IsObtainable.Should().BeTrue();
            section.Groups.Should().Contain(group => group.Title == "Store");
        }

        /// <summary>
        /// Scans Module\utm for the alphabetically-first store with at least one item, reading raw
        /// JSON directly (not <see cref="Documents.UtmDocument"/>) so this check cannot pass merely
        /// because it shares a bug with the index it verifies.
        /// </summary>
        private static (string StoreResRef, string StoreDisplay, string ItemResRef) FirstStoreSoldItem()
        {
            var utmDir = Path.Combine(CorpusLocator.ModuleDirectory, "utm");
            var files = Directory.EnumerateFiles(utmDir, "*.utm.json")
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;

                var storeResRef = GetString(root, "ResRef");
                var name = GetLocName(root);
                var display = string.IsNullOrWhiteSpace(name) ? storeResRef : name;

                if (!TryGetArray(root, "StoreList", out var pages))
                    continue;

                foreach (var page in pages.EnumerateArray())
                {
                    if (!TryGetArray(page, "ItemList", out var items))
                        continue;

                    foreach (var item in items.EnumerateArray())
                    {
                        var itemResRef = GetString(item, "InventoryRes");
                        if (!string.IsNullOrWhiteSpace(itemResRef))
                            return (storeResRef!, display!, itemResRef!);
                    }
                }
            }

            throw new InvalidOperationException("No store with at least one item was found in the module corpus.");
        }

        /// <summary>
        /// Scans the RecipeDefinition source tree for the first recipe whose fluent chain sets a
        /// <c>.Resref(...)</c> output within the same statement as its <c>_builder.Create(RecipeType.X, ...)</c>
        /// call - independent of <see cref="ItemObtainabilityIndex"/>'s own nearest-preceding-block regex.
        /// </summary>
        private static (string RecipeName, string ItemResRef) FirstRecipeOutput()
        {
            var recipeDir = Path.Combine(GameServerSourceRoot, "Feature", "RecipeDefinition");
            var pairRegex = new Regex(
                @"_builder\.Create\(\s*RecipeType\.(?<recipe>\w+)[^;]*?\.Resref\(\s*""(?<resref>[^""]+)""",
                RegexOptions.Compiled);

            var files = Directory.EnumerateFiles(recipeDir, "*.cs", SearchOption.AllDirectories)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var match = pairRegex.Match(File.ReadAllText(file));
                if (match.Success)
                    return (match.Groups["recipe"].Value, match.Groups["resref"].Value);
            }

            throw new InvalidOperationException("No recipe with a .Resref(...) output was found in the game source.");
        }

        private static bool TryGetArray(JsonElement element, string property, out JsonElement array)
        {
            array = default;
            return element.TryGetProperty(property, out var wrapper) &&
                   wrapper.TryGetProperty("value", out array) &&
                   array.ValueKind == JsonValueKind.Array;
        }

        private static string? GetString(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var wrapper) &&
                wrapper.TryGetProperty("value", out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private static string? GetLocName(JsonElement uti)
        {
            if (uti.TryGetProperty("LocName", out var ln) &&
                ln.TryGetProperty("value", out var value) &&
                value.TryGetProperty("0", out var first) &&
                first.ValueKind == JsonValueKind.String)
            {
                return first.GetString();
            }

            return null;
        }
    }
}
