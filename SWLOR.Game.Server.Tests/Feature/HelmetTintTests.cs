using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetTintTests
{
    [TestCase("helm_114", true, true, 1114)]
    [TestCase("HELM_114", true, true, 1114)]
    [TestCase("helm_034", true, true, 1034)]
    [TestCase("helm_114", false, true, 121)]
    [TestCase("helm_114", true, false, 121)]
    [TestCase("helm_999", true, true, 121)]
    [TestCase(null, true, true, 121)]
    public void HelmetRenderingRestoresTheCanonicalHeadWhenHiddenUnequippedOrUnsupported(
        string model, bool visible, bool parts, int expected)
    {
        var catalog = new HelmetModelCatalog(new[] { ("helm_114", 1114), ("helm_034", 1034) });
        Assert.That(catalog.ResolveHead(model, 121, visible, parts), Is.EqualTo(expected));
    }

    [Test]
    public void HelmetRenderingRejectsDuplicateHeadsAndModelsAndOutOfRangeIds()
    {
        Assert.Throws<ArgumentException>(() => new HelmetModelCatalog(new[] { ("helm_114", 1114), ("helm_034", 1114) }));
        Assert.Throws<ArgumentException>(() => new HelmetModelCatalog(new[] { ("helm_114", 1114), ("HELM_114", 1115) }));
        Assert.Throws<ArgumentException>(() => new HelmetModelCatalog(new[] { ("helm_114", 999) }));
        Assert.Throws<ArgumentException>(() => new HelmetModelCatalog(new[] { ("helm_114", 65536) }));
    }

    [TestCase("helm_114", 1u, 2u, true)]
    [TestCase("HELM_114", 1u, 2u, true)]
    [TestCase("helm_114", 1u, 1u, true)]
    [TestCase("pfh0_head103", 2u, 2u, true)]
    [TestCase("pfh0_chest249", 1u, 2u, true)]
    public void RgbAvailabilityFollowsTheRenderedAttachment(string model, uint item, uint creature, bool supported)
    {
        var selection = new TintMapMaterialSelection(model,
            new TintMapMaterialDefinition(model, model, new[] { TintMapLayerType.Cloth1 }),
            item, creature, true, AppearanceArmor.Invalid);
        Assert.That(RobeModelRenderer.SupportsRgb(selection), Is.EqualTo(supported));
    }
}
