using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

public class RobeAppearanceTests
{
    [Test]
    public void ChoicesUseTheWearersModelAndKeepTheirOriginalNumbers()
    {
        var assets = new HashSet<string> { "pmh0_robe018", "pmh0_robe020",
            "pfh0_robe019", "pfh0_robe024" };
        var styles = new[] { 0, 18, 19, 20, 24 };
        Assert.That(RobeAppearance.FilterStyles(styles, id => $"pmh0_robe{id:D3}", assets.Contains),
            Is.EqualTo(new[] { 0, 18, 20 }));
        Assert.That(RobeAppearance.FilterStyles(styles, id => $"pfh0_robe{id:D3}", assets.Contains),
            Is.EqualTo(new[] { 0, 19, 24 }));
    }

    [Test]
    public void NoneRemainsAvailableWhenTheModelCannotBeResolved()
    {
        Assert.That(RobeAppearance.FilterStyles(new[] { -1, 0, 19, 24, 0 }, _ => string.Empty,
            _ => throw new AssertionException("An unresolved model must never be queried")), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void ModelAvailabilityDoesNotRequireRgbMaterials()
    {
        Assert.That(RobeAppearance.FilterStyles(new[] { 0, 3, 187 }, id => $"pmh22_robe{id:D3}",
            model => model == "pmh22_robe003"), Is.EqualTo(new[] { 0, 3 }));
    }
}
