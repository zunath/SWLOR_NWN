using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class ModelResourceTests
{
    [TestCase("head_proxy", "helm_114\r\n", true)]
    [TestCase("head_proxy", "missing\n", false)]
    [TestCase("head_proxy", "HEAD_PROXY\n", false)]
    [TestCase("head_proxy", "../helm_114\n", false)]
    [TestCase("head_proxy", "\nhelm_114\n", false)]
    [TestCase("head_proxy", "", false)]
    public void LodModelsRequireAnAvailableFirstModelAndCannotFallBackToAShadowedMdl(
        string name, string contents, bool expected)
    {
        Assert.That(ModelResource.Exists(name,
            (model, type) => type == ResType.LOD ? model == "head_proxy" : model is "head_proxy" or "helm_114",
            _ => contents), Is.EqualTo(expected));
    }

    [Test]
    public void ChainsResolveToGeometryAndRejectCycles()
    {
        var links = new Dictionary<string, string> { ["first"] = "second", ["second"] = "last" };
        bool Available(string model, ResType type) => type == ResType.LOD ? links.ContainsKey(model) : model == "last";
        Assert.That(ModelResource.Exists("first", Available, model => links[model]), Is.True);
        links["second"] = "first";
        Assert.That(ModelResource.Exists("first", Available, model => links[model]), Is.False);
    }
}
