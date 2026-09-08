using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetTintTests
{
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
