using System;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Game.Server.Tests.Feature;

public class RobeModelCatalogTests
{
    private static RobeModelCatalog Catalog() => new(new[]
    {
        ("pfh0_robe187", 110, 0), ("pmh0_robe187", 110, 0), ("pfh0_robe001", 34, 0)
    }, new[] { (111, 0) });

    [TestCase(0, "pfh0_robe187", 110)]
    [TestCase(110, "PFH0_ROBE187", 110)]
    [TestCase(110, "pfh0_robe001", 34)]
    [TestCase(110, null, 0)]
    [TestCase(110, "missing", 0)]
    [TestCase(111, "missing", 0)]
    [TestCase(22, "pfh0_robe187", 22)]
    [TestCase(2, "pfh22_robe187", 2)]
    [TestCase(254, null, 254)]
    public void ResolvesRgbWithoutLosingTheBaseBody(int current, string model, int expected) =>
        Assert.That(Catalog().ResolvePhenotype(current, model), Is.EqualTo(expected));

    [Test]
    public void UntintedCataloguedRobesStillRenderThroughTheirBoundBodyRoot() =>
        Assert.That(Catalog().ResolvePhenotype(0, "pmh0_robe187"), Is.EqualTo(110));

    [TestCase(-1)]
    [TestCase(33)]
    [TestCase(256)]
    public void RejectsIdsOutsideReservedNativeByteRange(int phenotype) =>
        Assert.Throws<ArgumentException>(() => new RobeModelCatalog(new[] { ("pfh0_robe001", phenotype, 0) }));

    [Test]
    public void RejectsConflictingAssignments()
    {
        Assert.Throws<ArgumentException>(() => new RobeModelCatalog(new[]
            { ("pfh0_robe001", 34, 0), ("PFH0_ROBE001", 35, 0) }));
        Assert.Throws<ArgumentException>(() => new RobeModelCatalog(new[]
            { ("pfh0_robe001", 34, 0), ("pmh0_robe001", 34, 2) }));
    }
}
