using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Game.Server.Tests.Feature;

public class NeckAppearanceTests
{
    [Test]
    public void FallbackKeepsRaceGenderAndPrefersTheActualBody()
    {
        var models = new HashSet<string> { "pfh2_neck260", "pfh0_neck260", "pmh0_neck257", "pfe0_neck257" };
        Assert.That(NeckAppearance.ResolveModel("pfh2", 260, _ => 0, models.Contains), Is.EqualTo("pfh2_neck260"));
        Assert.That(NeckAppearance.ResolveModel("pfh34", 260, _ => 0, models.Contains), Is.EqualTo("pfh0_neck260"));
        Assert.That(NeckAppearance.ResolveModel("pfh0", 257, _ => 0, models.Contains), Is.Empty);
        Assert.That(NeckAppearance.ResolveModel("pfh0", 0, _ => 0, models.Contains), Is.Empty);
    }

    [Test]
    public void MissingFallbackCycleTerminates()
    {
        var visited = new List<string>();
        Assert.That(NeckAppearance.ResolveModel("pfd2", 159, p => p == 2 ? 0 : 2,
            model => { visited.Add(model); return false; }), Is.Empty);
        Assert.That(visited, Is.EqualTo(new[] { "pfd2_neck159", "pfd0_neck159" }));
    }

    [Test]
    public void RenderAliasesAreSpecificToSkeletonAndAuthoredSource()
    {
        var catalog = new NeckModelCatalog(new[] { ("pfh110", "pfh0_neck257", "pfh0_neck1000") });
        Assert.That(catalog.Resolve("PFH110", "PFH0_NECK257", 257), Is.EqualTo(1000));
        Assert.That(catalog.Resolve("pfh0", "pfh0_neck257", 257), Is.EqualTo(257));
        Assert.That(catalog.Resolve("pmh110", "pmh0_neck257", 257), Is.EqualTo(257));
        Assert.That(catalog.Resolve("pfh110", "pfh0_neck264", 264), Is.EqualTo(264));
    }

    [Test]
    public void ProjectedAliasesMapBackToTheirAuthoredStyle()
    {
        var catalog = new NeckModelCatalog(new[]
        {
            ("pfh110", "pfh0_neck257", "pfh0_neck1000"), ("pfh2", "pfh2_neck257", "pfh0_neck1001")
        });
        Assert.That(catalog.GetAuthoredStyle("pfh110", 1000), Is.EqualTo(257));
        Assert.That(catalog.GetAuthoredStyle("pfh0", 1001), Is.EqualTo(257));
        Assert.That(catalog.GetAuthoredStyle("pmh110", 1000), Is.EqualTo(1000));
        Assert.That(catalog.GetAuthoredStyle("pfh110", 257), Is.EqualTo(257));
        // Returning to the base body after a projected read must render the canonical style.
        Assert.That(catalog.Resolve("pfh0", "pfh0_neck257", catalog.GetAuthoredStyle("pfh0", 1000)), Is.EqualTo(257));
    }

    [Test]
    public void ShippedRenderTableLoadsAndEveryAliasRendersOneStyle()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        Assert.That(directory, Is.Not.Null, "Could not locate SWLOR_NWN repository root.");
        // The server loads this table at module start; an invalid row stops the module.
        var rows = new List<(string, string, string)>();
        foreach (var line in File.ReadLines(Path.Combine(directory!.FullName, "SWLOR_Haks", "sw_2da", "neckrender.2da")).Skip(3))
        {
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 4)
                rows.Add((fields[1], fields[2], fields[3]));
        }
        Assert.That(rows, Is.Not.Empty);
        var catalog = new NeckModelCatalog(rows);
        foreach (var (body, source, render) in rows)
        {
            var style = ushort.Parse(source[(source.LastIndexOf("neck", StringComparison.Ordinal) + 4)..]);
            var alias = ushort.Parse(render[(render.LastIndexOf("neck", StringComparison.Ordinal) + 4)..]);
            Assert.That(catalog.Resolve(body, source, style), Is.EqualTo(alias));
            Assert.That(catalog.GetAuthoredStyle(body, alias), Is.EqualTo(style));
        }
    }

    [Test]
    public void RejectsAliasesRenderingDifferentStyles() => Assert.Throws<ArgumentException>(() => new NeckModelCatalog(new[]
    {
        ("pfh110", "pfh0_neck257", "pfh0_neck1000"), ("pfh111", "pfh0_neck260", "pfh0_neck1000")
    }));

    [TestCase("pfh110", "pfh0_neck257", "pmh0_neck1000")]
    [TestCase("pmh110", "pfh0_neck257", "pfh0_neck1000")]
    [TestCase("pfh110", "pfh0_neck257", "pfh0_neck999")]
    [TestCase("pfh110", "pfh0_neck257", "pfh0_neck65536")]
    [TestCase("pfh110", "pfh0_neck1000", "pfh0_neck1001")]
    public void RejectsInvalidAliases(string body, string source, string render) =>
        Assert.Throws<ArgumentException>(() => new NeckModelCatalog(new[] { (body, source, render) }));

    [Test]
    public void RejectsAmbiguousMappings() => Assert.Throws<ArgumentException>(() => new NeckModelCatalog(new[]
    {
        ("pfh110", "pfh0_neck257", "pfh0_neck1000"), ("PFH110", "PFH0_NECK257", "pfh0_neck1001")
    }));
}
