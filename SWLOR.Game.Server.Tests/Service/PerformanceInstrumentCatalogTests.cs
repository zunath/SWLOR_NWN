using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;

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
    public void The_resrefs_are_the_five_instruments_in_the_order_they_were_listed()
    {
        PerformanceInstrumentCatalog.Resrefs.Should().Equal("guitar", "lute", "harp", "kloo_horn", "slitherhorn");
    }
}
