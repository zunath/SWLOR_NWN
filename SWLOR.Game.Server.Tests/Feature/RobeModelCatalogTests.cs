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

    [TestCase(0, "pfh0_robe187", true, 110)]
    [TestCase(110, "PFH0_ROBE187", true, 110)]
    [TestCase(110, "pfh0_robe001", true, 34)]
    [TestCase(110, "pfh0_robe187", false, 0)]
    [TestCase(110, null, true, 0)]
    [TestCase(110, "missing", true, 0)]
    [TestCase(111, "missing", true, 0)]
    [TestCase(22, "pfh0_robe187", true, 22)]
    [TestCase(2, "pfh22_robe187", true, 2)]
    [TestCase(254, null, false, 254)]
    public void ResolvesRgbWithoutLosingTheBaseBody(int current, string model, bool rgb, int expected) =>
        Assert.That(Catalog().ResolvePhenotype(current, model, rgb), Is.EqualTo(expected));

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
