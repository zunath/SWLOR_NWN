using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShoulderAppearanceTests
{
    [Test]
    public void ProjectionIsSpecificToBodyAndShoulderSide()
    {
        var catalog = new ShoulderModelCatalog(new[]
        {
            ("pmh183", "pmh0_shol013", "pmh0_shol1000"),
            ("pmh183", "pmh0_shor013", "pmh0_shor1006")
        });
        Assert.That(catalog.Resolve("PMH183", "PMH0_SHOL013", 13), Is.EqualTo(1000));
        Assert.That(catalog.Resolve("pmh183", "pmh0_shor013", 13), Is.EqualTo(1006));
        Assert.That(catalog.Resolve("pmh0", "pmh0_shol013", 13), Is.EqualTo(13));
        Assert.That(catalog.Resolve("pfh183", "pfh0_shol013", 13), Is.EqualTo(13));
        Assert.That(catalog.Resolve("pme183", "pme0_shol013", 13), Is.EqualTo(13));
        Assert.That(catalog.Resolve("pmh183", "pmh0_shol014", 14), Is.EqualTo(14));
        Assert.That(catalog.GetAuthoredStyle("pmh0", AppearanceArmor.LeftShoulder, 1000), Is.EqualTo(13));
        Assert.That(catalog.GetAuthoredStyle("pmh183", AppearanceArmor.RightShoulder, 1000), Is.EqualTo(1000));
        Assert.That(catalog.GetAuthoredStyle("pfh183", AppearanceArmor.LeftShoulder, 1000), Is.EqualTo(1000));
        Assert.That(catalog.GetAuthoredStyle("pmh183", AppearanceArmor.Neck, 1000), Is.EqualTo(1000));
    }

    [Test]
    public void ShippedTableCoversAllSixStylesOnBothSidesForEveryMaleHumanRobe()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        var haks = Path.Combine(root!.FullName, "SWLOR_Haks");
        var rows = File.ReadLines(Path.Combine(haks, "sw_2da", "shoulderrender.2da")).Skip(3)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(fields => (Body: fields[1], Source: fields[2], Render: fields[3])).ToArray();
        var catalog = new ShoulderModelCatalog(rows);
        var bodies = File.ReadLines(Path.Combine(haks, "sw_2da", "roberender.2da")).Skip(3)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(fields => fields[1].StartsWith("pmh0_robe", StringComparison.Ordinal))
            .Select(fields => "pmh" + fields[2]).Distinct();
        foreach (var body in bodies)
        foreach (var side in new[] { 'l', 'r' })
        foreach (var style in new ushort[] { 13, 140, 141, 185, 256, 260 })
        {
            var source = $"pmh0_sho{side}{style:D3}";
            var render = catalog.Resolve(body, source, style);
            Assert.That(render, Is.GreaterThanOrEqualTo(1000), body + "/" + source);
            Assert.That(catalog.GetAuthoredStyle(body,
                side == 'l' ? AppearanceArmor.LeftShoulder : AppearanceArmor.RightShoulder, render), Is.EqualTo(style));
            Assert.That(File.Exists(Path.Combine(haks, side == 'l' ? "sw_pt_lshoul" : "sw_pt_rshoul",
                $"pmh0_sho{side}{render}.mdl")), Is.True);
        }
    }

    [TestCase("pmh183", "pmh0_shol013", "pmh0_shor1000")]
    [TestCase("pfh183", "pmh0_shol013", "pmh0_shol1000")]
    [TestCase("pmh183", "pmh0_shol013", "pmh0_shol999")]
    [TestCase("pmh183", "pmh0_shol013", "pmh0_shol65536")]
    [TestCase("pmh183", "pmh0_shol1000", "pmh0_shol1001")]
    public void RejectsInvalidRows(string body, string source, string render) =>
        Assert.Throws<ArgumentException>(() => new ShoulderModelCatalog(new[] { (body, source, render) }));

    [Test]
    public void RejectsAliasesForDifferentAuthoredStyles() => Assert.Throws<ArgumentException>(() =>
        new ShoulderModelCatalog(new[]
        {
            ("pmh183", "pmh0_shol013", "pmh0_shol1000"),
            ("pmh184", "pmh0_shol140", "pmh0_shol1000")
        }));
}
