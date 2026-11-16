using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The writes of tint colours to the engine's material uniforms. The full creature pass drives the native server
/// library, so the engine tests cover it; these read the item pass and the single-material write.
/// </summary>
[TestFixture]
public class TintMapApplyServiceTests
{
    private const uint Creature = 10;
    private const uint Item = 11;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private TintMapApplyService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature);
        _world.Add(Item, ObjectType.Item);
        var droid = ScriptedProxy.Create<IDroidService>(out _);
        var twin = ScriptedProxy.Create<ITwinPistolEquipmentService>(out _);
        _service = new TintMapApplyService(droid, twin);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static TintMapMaterialSelection Selection(TintMapLayerType layer = TintMapLayerType.Cloth1) =>
        new("model", new TintMapMaterialDefinition("Material", "mat", layer), Item, Creature, usesItemColors: true,
            AppearanceArmor.Torso);

    [Test]
    public void ApplyColor_WritesOneUniformForTheLayerOfTheMaterial()
    {
        _service.ApplyColor(Creature, Selection(), TintMapLayerType.Cloth1, new TintMapColorSelection(12, null));

        var write = _world.UniformWrites().Should().ContainSingle().Subject;
        write.Object.Should().Be(Creature);
        write.Material.Should().Be("mat");
        write.Parameter.Should().Be(TintMapMaterialRegistry.GetLayer(TintMapLayerType.Cloth1).UniformName);
    }

    [Test]
    public void ApplyColor_EncodesACustomColorAndAPaletteRowDifferently()
    {
        _service.ApplyColor(Creature, Selection(), TintMapLayerType.Cloth1, new TintMapColorSelection(12, null));
        _service.ApplyColor(Creature, Selection(), TintMapLayerType.Cloth1, new TintMapColorSelection(12, new TintMapColor(1, 2, 3)));

        var values = _world.Engine.CallsTo("SetMaterialShaderUniformVec4").Select(call => (float)call.Arguments[3]!).ToList();
        values[0].Should().BeApproximately(TintMapMaterialRegistry.GetPaletteCoordinate(TintMapLayerType.Cloth1, 12), 0f);
        values[1].Should().NotBeApproximately(values[0], 0.0001f);
    }

    [Test]
    public void ApplyCurrentItemColors_ClearsTheShaderOverridesOfADroppedItem()
    {
        _service.ApplyCurrentItemColors(Item);

        _world.Engine.CallsTo("ResetMaterialShaderUniforms").Should().ContainSingle(call => (uint)call.Arguments[0]! == Item);
    }

    [Test]
    public void ApplyCurrentItemColors_IgnoresInvalidObjectsAndNonItems()
    {
        _service.ApplyCurrentItemColors(999);
        _service.ApplyCurrentItemColors(Creature);

        _world.Engine.CallsTo("ResetMaterialShaderUniforms").Should().BeEmpty();
    }

    [Test]
    public void ApplyCurrentItemColors_IgnoresAnItemSomeoneCarries()
    {
        _world.Engine.SetReturnHandler("GetItemPossessor", _ => Creature);

        _service.ApplyCurrentItemColors(Item);

        _world.Engine.CallsTo("ResetMaterialShaderUniforms").Should().BeEmpty();
    }
}
