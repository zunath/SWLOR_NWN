using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Game.Server.Tests.Feature;

public class MountedTintModelTests
{
    [TestCase("pmd22_head100", "pmd0_head100")]
    [TestCase("pfh22_chest208", "pfh0_chest208")]
    [TestCase("pfe22_handl003", "pfe0_handl003")]
    public void MissingMountedPartsUseTheSameRaceAndGenderFallback(string mounted, string ordinary)
    {
        var models = new HashSet<string> { ordinary };
        Assert.That(TintMapModelResolver.ResolveMountedBodyPartModel(mounted, _ => 0, models.Contains),
            Is.EqualTo(ordinary));
    }

    [Test]
    public void ExplicitMountedPartKeepsItsOwnMaterials()
    {
        var models = new HashSet<string> { "pfh22_head018", "pfh0_head018" };
        Assert.That(TintMapModelResolver.ResolveMountedBodyPartModel("pfh22_head018",
            _ => throw new AssertionException("An explicit mounted model must not use its fallback"), models.Contains),
            Is.EqualTo("pfh22_head018"));
    }

    [Test]
    public void MissingFallbackCycleProducesNoTintSelection()
    {
        var checkedModels = new List<string>();
        var otherRaceAndGender = new HashSet<string> { "pmd0_head100", "pfh0_head100" };
        Assert.That(TintMapModelResolver.ResolveMountedBodyPartModel("pfd22_head100", p => p == 22 ? 0 : 22,
            model => { checkedModels.Add(model); return otherRaceAndGender.Contains(model); }), Is.Empty);
        Assert.That(checkedModels, Is.EqualTo(new[] { "pfd22_head100", "pfd0_head100" }));
    }

    [TestCase("pmh22_robe187")]
    [TestCase("pfh0_head001")]
    [TestCase("pmd2_chest208")]
    [TestCase("pfe34_handl003")]
    public void RobesAndOtherPhenotypesKeepTheirExistingResolution(string model)
    {
        Assert.That(TintMapModelResolver.ResolveMountedBodyPartModel(model,
            _ => throw new AssertionException("This model must not use mounted fallback"),
            _ => throw new AssertionException("This model must not query mounted resources")), Is.EqualTo(model));
    }
}
