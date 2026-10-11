using System;
using System.Collections.Generic;
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
