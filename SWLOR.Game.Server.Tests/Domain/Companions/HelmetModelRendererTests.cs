using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>The managed rules of rendering a helmet through the head, the model resource lookup and the cloak row.</summary>
[TestFixture]
public class HelmetModelRendererTests
{
    private const uint Creature = 10;
    private const uint Helmet = 11;

    private TintMapWorld _world = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _world.Add(Creature, ObjectType.Creature);
        _world.Add(Helmet, ObjectType.Item);
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    [TestCase("helm_007", true, true, 1007)]
    [TestCase("HELM_012", true, true, 1012)]
    [TestCase("helm_007", false, true, 5)]
    [TestCase("helm_007", true, false, 5)]
    [TestCase("helm_000", true, true, 5)]
    [TestCase("helm_7", true, true, 5)]
    [TestCase("cloak_007", true, true, 5)]
    [TestCase(null, true, true, 5)]
    public void ResolveHead_UsesTheRenderHeadOnlyForAVisibleTintableHelmetOnAPartsBody(string? model, bool visible, bool parts, int expected)
    {
        HelmetModelRenderer.ResolveHead(model!, 5, visible, parts).Should().Be((ushort)expected);
    }

    [Test]
    public void IsRenderedHead_AcceptsOnlyTheReservedRange()
    {
        HelmetModelRenderer.IsRenderedHead(1000).Should().BeFalse();
        HelmetModelRenderer.IsRenderedHead(1001).Should().BeTrue();
        HelmetModelRenderer.IsRenderedHead(1999).Should().BeTrue();
        HelmetModelRenderer.IsRenderedHead(2000).Should().BeFalse();
        HelmetModelRenderer.GetHeadModel('m', "H", 1007).Should().Be("pmh0_head1007");
    }

    [Test]
    public void ShownByOwner_FollowsTheHiddenFlagUnlessTheRenderHeadSetIt()
    {
        _world.Engine.SetReturn("GetHiddenWhenEquipped", 0);
        HelmetModelRenderer.IsShownByOwner(Helmet).Should().BeTrue();

        _world.Engine.SetReturn("GetHiddenWhenEquipped", 1);
        HelmetModelRenderer.IsShownByOwner(Helmet).Should().BeFalse();

        _world.SetInt(Helmet, "HELMET_RENDER_HEAD_HIDDEN", 1);
        HelmetModelRenderer.IsShownByOwner(Helmet).Should().BeTrue();
    }

    [Test]
    public void SetShownByOwner_ClearsTheRenderHeadMarkAndWritesTheHiddenFlag()
    {
        _world.SetInt(Helmet, "HELMET_RENDER_HEAD_HIDDEN", 1);

        HelmetModelRenderer.SetShownByOwner(Helmet, shown: false);

        _world.LocalsOf(Helmet).Should().NotContainKey("HELMET_RENDER_HEAD_HIDDEN");
        _world.Engine.CallsTo("SetHiddenWhenEquipped").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Helmet, true);
    }

    [Test]
    public void Release_RestoresTheNativeFlagOnlyForAHelmetThatRenderedThroughTheHead()
    {
        HelmetModelRenderer.Release(Helmet);
        _world.Engine.CallsTo("SetHiddenWhenEquipped").Should().BeEmpty();

        _world.SetInt(Helmet, "HELMET_RENDER_HEAD_HIDDEN", 1);
        HelmetModelRenderer.Release(Helmet);

        _world.Engine.CallsTo("SetHiddenWhenEquipped").Should().ContainSingle().Which.Arguments.Should().Equal(Helmet, false);
        _world.LocalsOf(Helmet).Should().NotContainKey("HELMET_RENDER_HEAD_HIDDEN");
    }

    [Test]
    public void SupportsRgb_IsTrueForAnythingButAWornHelmetWithoutARenderHead()
    {
        var other = new TintMapMaterialSelection("pmh0_chest001",
            new TintMapMaterialDefinition("M", "mat", TintMapLayerType.Cloth1), Creature, Creature, false, AppearanceArmor.Invalid);
        HelmetModelRenderer.SupportsRgb(other).Should().BeTrue();

        var worn = new TintMapMaterialSelection("helm_007",
            new TintMapMaterialDefinition("M", "mat", TintMapLayerType.Metal1), Helmet, Creature, true, AppearanceArmor.Invalid);
        worn.IsWornHelmet.Should().BeTrue();
        HelmetModelRenderer.SupportsRgb(worn).Should().BeFalse("the wearer's body is not a parts body");

        _world.Set2DA("appearance", "MODELTYPE", 0, "P");
        _world.Set2DA("appearance", "RACE", 0, "H");
        HelmetModelRenderer.SupportsRgb(worn).Should().BeFalse("no head model resource exists in the unit environment");
        HelmetModelRenderer.RendersThroughHead(Creature, new[] { worn }).Should().BeFalse();
        HelmetModelRenderer.RendersThroughHead(Creature, new[] { other }).Should().BeFalse();
    }

    [Test]
    public void SupportsRgb_RejectsAHelmetModelWithoutARaceOrOnAnotherModelType()
    {
        var worn = new TintMapMaterialSelection("helm_007",
            new TintMapMaterialDefinition("M", "mat", TintMapLayerType.Metal1), Helmet, Creature, true, AppearanceArmor.Invalid);
        _world.Set2DA("appearance", "MODELTYPE", 0, "P");
        _world.Set2DA("appearance", "RACE", 0, "HH");

        HelmetModelRenderer.SupportsRgb(worn).Should().BeFalse();
    }

    [Test]
    public void ModelResource_FollowsLodRedirectsAndStopsOnMissingOrCyclicResources()
    {
        var lods = new Dictionary<string, string> { ["start"] = "middle\nignored", ["middle"] = "end" };
        var models = new HashSet<string> { "end" };
        bool Has(string name, ResType type) => type == ResType.LOD ? lods.ContainsKey(name) : models.Contains(name);
        string Read(string name) => lods[name];

        ModelResource.Exists("start", Has, Read).Should().BeTrue();
        ModelResource.Exists("end", Has, Read).Should().BeTrue();
        ModelResource.Exists("missing", Has, Read).Should().BeFalse();
        ModelResource.Exists("", Has, Read).Should().BeFalse();
        ModelResource.Exists("bad name!", Has, Read).Should().BeFalse();
        ModelResource.Exists("a_very_long_model_name_over_sixteen", Has, Read).Should().BeFalse();

        lods["empty"] = "  ";
        ModelResource.Exists("empty", Has, Read).Should().BeFalse();

        lods["loop_a"] = "loop_b";
        lods["loop_b"] = "loop_a";
        ModelResource.Exists("loop_a", Has, Read).Should().BeFalse();
    }

    [Test]
    public void CloakTintRenderer_WritesTheTintRowOfTheWornCloakTextureOnlyWhenACloakMaterialIsPresent()
    {
        var cloakMaterial = new TintMapMaterialSelection("cloak_002",
            new TintMapMaterialDefinition("Cloak", CloakTintRenderer.MaterialResref, TintMapLayerType.Leather1), Helmet, Creature,
            true, AppearanceArmor.Invalid);
        var other = new TintMapMaterialSelection("model",
            new TintMapMaterialDefinition("M", "mat", TintMapLayerType.Cloth1), Creature, Creature, false, AppearanceArmor.Invalid);
        _world.Equip(Creature, InventorySlot.Cloak, Helmet);
        _world.SetAppearance(Helmet, ItemAppearanceType.SimpleModel, 0, 2);
        _world.Set2DA("cloakmodel", "TEXTURE", 2, "6");
        _world.Set2DA("cloaktint", "INDEX", 6, "9");

        CloakTintRenderer.Apply(Creature, new[] { other });
        _world.UniformWrites().Should().BeEmpty();

        CloakTintRenderer.Apply(Creature, new[] { cloakMaterial });
        _world.UniformWrites().Should().ContainSingle().Which.Should().Be((Creature, CloakTintRenderer.MaterialResref, "cloakTexture"));

        _world.Set2DA("cloaktint", "INDEX", 6, "x");
        CloakTintRenderer.Apply(Creature, new[] { cloakMaterial });
        _world.UniformWrites().Should().HaveCount(1);
    }
}
