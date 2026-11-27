using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class PerformanceInstrumentCatalogTests
{
    // baseitems.2da row "fashionacc": every held instrument is this base item, as are the held props below.
    private const int HeldAccessoryBaseItem = 314;

    // Held accessories that stores sell but that are not played: props, and the violin's bow.
    private static readonly HashSet<string> HeldPropsThatAreNotInstruments = new(StringComparer.OrdinalIgnoreCase)
    {
        "book_a", "book_b",
        "foldfan_1", "foldfan_blk", "foldfan_pink", "foldfan_purp", "foldfan_white",
        "gold_orb", "handdatapad",
        "umbrella_black01", "umbrella_drg", "umbrella_frame", "umbrella_yel",
        "violin_bow",
    };

    [TestCase("guitar", PerformanceInstrumentFamily.Strings)]
    [TestCase("lute", PerformanceInstrumentFamily.Strings)]
    [TestCase("harp", PerformanceInstrumentFamily.Strings)]
    [TestCase("kloo_horn", PerformanceInstrumentFamily.Horns)]
    [TestCase("slitherhorn", PerformanceInstrumentFamily.Horns)]
    [TestCase("GUITAR", PerformanceInstrumentFamily.Strings)]
    [TestCase("Kloo_Horn", PerformanceInstrumentFamily.Horns)]
    public void An_instrument_resref_has_its_family_without_regard_to_case(string resref, PerformanceInstrumentFamily family)
    {
        PerformanceInstrumentCatalog.TryGetFamily(resref, out var found).Should().BeTrue();
        found.Should().Be(family);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("flute")]
    [TestCase("guitar ")]
    public void Anything_else_has_no_family(string? resref)
    {
        PerformanceInstrumentCatalog.TryGetFamily(resref!, out var found).Should().BeFalse();
        found.Should().Be(PerformanceInstrumentFamily.None);
    }

    [Test]
    public void The_resrefs_are_the_ten_instruments()
    {
        PerformanceInstrumentCatalog.Resrefs.Should().BeEquivalentTo("guitar", "lute", "harp", "kloo_horn", "slitherhorn", "jessoon", "night_microplain", "night_microbrand", "panpipes",
            "violin_m", "violin_f", "tambourine");
    }

    [TestCase("night_microplain", PerformanceInstrumentFamily.Vocals)]
    [TestCase("NIGHT_MICROBRAND", PerformanceInstrumentFamily.Vocals)]
    [TestCase("panpipes", PerformanceInstrumentFamily.Pipes)]
    [TestCase("violin_m", PerformanceInstrumentFamily.BowedStrings)]
    [TestCase("violin_f", PerformanceInstrumentFamily.BowedStrings)]
    [TestCase("tambourine", PerformanceInstrumentFamily.Percussion)]
    [TestCase("kloo_horn", PerformanceInstrumentFamily.Horns)]
    [TestCase("slitherhorn", PerformanceInstrumentFamily.Horns)]
    [TestCase("jessoon", PerformanceInstrumentFamily.Horns)]
    [TestCase("guitar", PerformanceInstrumentFamily.Strings)]
    public void ExistingItemsAreRecognizedWithoutItemLocals(string resref, PerformanceInstrumentFamily expected)
    {
        PerformanceInstrumentCatalog.TryGetFamily(resref, out var family).Should().BeTrue();
        family.Should().Be(expected);
    }

    [TestCase("violin_bow")]
    [TestCase("unrelated_item")]
    [TestCase("")]
    public void PropsThatCannotPerformAloneAreNotInstruments(string resref)
    {
        PerformanceInstrumentCatalog.TryGetFamily(resref, out var family).Should().BeFalse();
        family.Should().Be(PerformanceInstrumentFamily.None);
    }

    /// <summary>
    /// A sold instrument missing from the catalog cannot be played (the Flanged Jessoon was). A new held accessory in a store
    /// must be added to the catalog, or to the props list above when it is not an instrument.
    /// </summary>
    [Test]
    public void EveryHeldAccessoryStoresSell_IsAnInstrumentOrAKnownProp()
    {
        var module = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var unclassified = new List<string>();

        foreach (var store in Directory.GetFiles(Path.Combine(module, "utm"), "*.utm.json"))
        {
            foreach (var resref in SoldResrefs(JToken.Parse(File.ReadAllText(store))).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var blueprint = Path.Combine(module, "uti", $"{resref}.uti.json");
                if (!File.Exists(blueprint))
                    continue;

                var baseItem = (int?)JObject.Parse(File.ReadAllText(blueprint))["BaseItem"]?["value"];
                if (baseItem != HeldAccessoryBaseItem ||
                    PerformanceInstrumentCatalog.TryGetFamily(resref, out _) ||
                    HeldPropsThatAreNotInstruments.Contains(resref))
                    continue;

                unclassified.Add($"{resref} ({Path.GetFileName(store)})");
            }
        }

        unclassified.Should().BeEmpty("every held accessory a store sells is either playable or a known prop");
    }

    [Test]
    public void EveryCataloguedInstrument_IsSoldInTheAccessoryStore()
    {
        var store = Path.Combine(FindRepositoryRoot().FullName, "Module", "utm", "night_accessory.utm.json");
        var sold = SoldResrefs(JToken.Parse(File.ReadAllText(store))).ToHashSet(StringComparer.OrdinalIgnoreCase);

        PerformanceInstrumentCatalog.Resrefs.Where(resref => !sold.Contains(resref)).Should().BeEmpty();
    }

    private static IEnumerable<string> SoldResrefs(JToken store) =>
        store.SelectTokens("$..InventoryRes.value").Select(token => (string)token!).Where(resref => !string.IsNullOrEmpty(resref));

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
