using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class PartSkinAppearanceTests
{
    private static readonly (string, string)[] Layouts = { ("pfh47", "2"), ("pfh216", "2"), ("pmh47", "6") };

    [Test]
    public void ProjectionIsSpecificToLayoutPartAndStyle()
    {
        var catalog = new PartSkinModelCatalog(Layouts, new[]
        {
            ("2", "pfh0_pelvis010", "pfh0_pelvis1162"),
            ("2", "pfh0_handl003", "pfh0_handl1156"),
            ("6", "pmh0_pelvis010", "pmh0_pelvis1400")
        });
        Assert.That(catalog.Resolve("PFH47", AppearanceArmor.Pelvis, 10), Is.EqualTo(1162));
        Assert.That(catalog.Resolve("pfh216", AppearanceArmor.Pelvis, 10), Is.EqualTo(1162));
        Assert.That(catalog.Resolve("pmh47", AppearanceArmor.Pelvis, 10), Is.EqualTo(1400));
        Assert.That(catalog.Resolve("pfh47", AppearanceArmor.LeftHand, 3), Is.EqualTo(1156));
        Assert.That(catalog.Resolve("pfh47", AppearanceArmor.RightHand, 3), Is.EqualTo(3));
        Assert.That(catalog.Resolve("pfh47", AppearanceArmor.Pelvis, 11), Is.EqualTo(11));
        Assert.That(catalog.Resolve("pfh47", AppearanceArmor.Pelvis, 0), Is.EqualTo(0));
        Assert.That(catalog.Resolve("pfh0", AppearanceArmor.Pelvis, 10), Is.EqualTo(10));
        Assert.That(catalog.Resolve("pfe47", AppearanceArmor.Pelvis, 10), Is.EqualTo(10));
        Assert.That(catalog.Resolve(null, AppearanceArmor.Pelvis, 10), Is.EqualTo(10));
    }

    [Test]
    public void PartNamesMatchEveryModularSlotExceptNeckAndRobe()
    {
        var expected = Enum.GetValues<AppearanceArmor>()
            .Where(part => part is not (AppearanceArmor.Invalid or AppearanceArmor.Num or AppearanceArmor.Neck or AppearanceArmor.Robe));
        Assert.That(PartSkinModelCatalog.PartNames.Keys, Is.EquivalentTo(expected));
        Assert.That(PartSkinModelRenderer.Parts, Is.EquivalentTo(expected));
    }

    [Test]
    public void ShippedTablesCoverTheReportedPartsOnEveryRobeFamilyBody()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        var haks = Path.Combine(root!.FullName, "SWLOR_Haks");
        string[][] Read(string table) => File.ReadLines(Path.Combine(haks, "sw_2da", table)).Skip(3)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
        var layouts = Read("partlayout.2da").Select(fields => (fields[1], fields[2])).ToArray();
        var renders = Read("partrender.2da").Select(fields => (fields[1], fields[2], fields[3])).ToArray();
        var catalog = new PartSkinModelCatalog(layouts, renders);
        var robeBodies = Read("roberender.2da").Select(fields => fields[1][..3] + fields[2]).ToHashSet();
        Assert.That(layouts.Select(layout => layout.Item1), Is.SubsetOf(robeBodies));

        var directories = PartSkinModelCatalog.PartNames.ToDictionary(pair => pair.Value, pair => pair.Key switch
        {
            AppearanceArmor.Torso => "sw_pt_chest",
            AppearanceArmor.Pelvis => "sw_pt_pelvis",
            AppearanceArmor.Belt => "sw_pt_belt",
            AppearanceArmor.LeftHand => "sw_pt_lhand",
            AppearanceArmor.RightHand => "sw_pt_rhand",
            AppearanceArmor.LeftShin => "sw_pt_lshin",
            AppearanceArmor.RightShin => "sw_pt_rshin",
            AppearanceArmor.LeftShoulder => "sw_pt_lshoul",
            AppearanceArmor.RightShoulder => "sw_pt_rshoul",
            _ => string.Empty
        });
        foreach (var (_, _, render) in renders)
        {
            var part = Regex.Match(render, @"^p[fm][a-z]0_([a-z]+)\d+$").Groups[1].Value;
            Assert.That(File.Exists(Path.Combine(haks, directories[part], render + ".mdl")), Is.True, render);
        }

        foreach (var gender in new[] { 'f', 'm' })
        foreach (var phenotype in new[] { 47, 216, 200 })
        foreach (var (part, style) in new[] { (AppearanceArmor.Pelvis, 10), (AppearanceArmor.Torso, 3),
                     (AppearanceArmor.LeftHand, 3), (AppearanceArmor.RightHand, 3) })
        {
            var body = $"p{gender}h{phenotype}";
            Assert.That(catalog.Resolve(body, part, (ushort)style), Is.GreaterThanOrEqualTo(1000), $"{body}/{part}/{style}");
            Assert.That(catalog.Resolve($"p{gender}h0", part, (ushort)style), Is.EqualTo(style));
        }
    }

    [TestCase("pfh47", "2", "2", "pfh0_pelvis010", "pfh0_pelvis999")]
    [TestCase("pfh47", "2", "2", "pfh0_pelvis010", "pfh0_pelvis65536")]
    [TestCase("pfh47", "2", "2", "pfh0_pelvis010", "pfh0_chest1000")]
    [TestCase("pfh47", "2", "2", "pfh0_pelvis010", "pmh0_pelvis1000")]
    [TestCase("pfh47", "2", "2", "pmh0_pelvis010", "pmh0_pelvis1000")]
    [TestCase("pfh47", "2", "3", "pfh0_pelvis010", "pfh0_pelvis1000")]
    [TestCase("pfh47", "2", "2", "pfh0_neck010", "pfh0_neck1000")]
    [TestCase("pfh47", "2", "2", "pfh0_robe010", "pfh0_robe1000")]
    [TestCase("pfh47", "2", "2", "pfh0_pelvis1000", "pfh0_pelvis1001")]
    [TestCase("robe", "2", "2", "pfh0_pelvis010", "pfh0_pelvis1000")]
    public void RejectsInvalidRows(string body, string layout, string renderLayout, string source, string render) =>
        Assert.Throws<ArgumentException>(() => new PartSkinModelCatalog(new[] { (body, layout) },
            new[] { (renderLayout, source, render) }));

    [Test]
    public void RejectsDuplicateAndConflictingRows()
    {
        Assert.Throws<ArgumentException>(() => new PartSkinModelCatalog(
            new[] { ("pfh47", "2"), ("pfh47", "3") }, Array.Empty<(string, string, string)>()));
        Assert.Throws<ArgumentException>(() => new PartSkinModelCatalog(
            new[] { ("pfh47", "2"), ("pmh47", "2") }, Array.Empty<(string, string, string)>()));
        Assert.Throws<ArgumentException>(() => new PartSkinModelCatalog(Layouts, new[]
        {
            ("2", "pfh0_pelvis010", "pfh0_pelvis1000"),
            ("2", "pfh0_pelvis010", "pfh0_pelvis1001")
        }));
        Assert.Throws<ArgumentException>(() => new PartSkinModelCatalog(Layouts, new[]
        {
            ("2", "pfh0_pelvis010", "pfh0_pelvis1000"),
            ("2", "pfh0_pelvis011", "pfh0_pelvis1000")
        }));
    }
}
