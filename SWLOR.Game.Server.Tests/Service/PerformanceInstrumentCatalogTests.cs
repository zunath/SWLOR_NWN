using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceInstrumentCatalogTests
{
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
