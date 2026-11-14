using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class PerformanceInstrumentCatalogTests
{
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
        PerformanceInstrumentCatalog.Resrefs.Should().BeEquivalentTo("guitar", "lute", "harp", "kloo_horn", "slitherhorn", "night_microplain", "night_microbrand", "panpipes",
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
}
