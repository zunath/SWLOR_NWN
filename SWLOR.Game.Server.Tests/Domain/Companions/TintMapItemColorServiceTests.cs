using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>Setting and resetting the colours of an item's materials.</summary>
[TestFixture]
public class TintMapItemColorServiceTests
{
    private const uint Creature = 10;
    private const uint Item = 11;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private ITintMapStateService _state = null!;
    private ScriptedProxy _apply = null!;
    private ScriptedProxy _droid = null!;
    private ScriptedProxy _droidOverrides = null!;
    private TintMapItemColorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _state = _container.Get<ITintMapStateService>();
        _world.Add(Creature, ObjectType.Creature);
        _world.Add(Item, ObjectType.Item);
        var apply = ScriptedProxy.Create<ITintMapApplyService>(out _apply);
        _apply.On("ApplyColor", _ => null).On("ApplyCurrentColorsAndPublish", _ => null);
        var droid = ScriptedProxy.Create<IDroidService>(out _droid);
        _droid.On("UpdateEquippedItemSnapshot", _ => null);
        var droidOverrides = ScriptedProxy.Create<ITintMapDroidOverrideService>(out _droidOverrides);
        _droidOverrides.On("SaveDroidOverride", _ => null);
        _service = new TintMapItemColorService(apply, droid, droidOverrides, _state);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static string Variable(string material, TintMapLayerType layer = TintMapLayerType.Cloth1) =>
        TintMapVariable.GetName(material, layer);

    private static TintMapMaterialSelection Selection(string material, AppearanceArmor part = AppearanceArmor.Torso,
        TintMapLayerType layer = TintMapLayerType.Cloth1) =>
        new("model", new TintMapMaterialDefinition("Material", material, layer), Item, Creature, usesItemColors: true, part);

    private int Calls(ScriptedProxy proxy, string member) => proxy.Calls.Count(call => call.Member == member);

    [Test]
    public void SetColor_StoresTheColorOnThePaletteSourceAppliesItAndPublishes()
    {
        var color = new TintMapColor(30, 40, 50);
        var selection = Selection("a");

        _service.SetColor(Creature, selection, TintMapLayerType.Cloth1, color);

        _world.Int(Item, Variable("a")).Should().Be(color.ToStoredValue());
        _droidOverrides.Calls.Should().ContainSingle(call => call.Member == "SaveDroidOverride"
            && (int)call.Arguments[4]! == color.ToStoredValue());
        _apply.Calls.Should().Contain(call => call.Member == "ApplyColor" && ((TintMapColorSelection)call.Arguments[3]!).CustomColor == color);
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(1);
    }

    [Test]
    public void SetColor_InvalidatesAPendingCarryForItsLayerButSetStoredColorCanSkipThat()
    {
        var registration = _state.RegisterPendingItemColorCarry(99, Item, AppearanceArmor.Torso, new[] { TintMapLayerType.Cloth1 });

        _service.SetStoredColor(Creature, Selection("a"), TintMapLayerType.Cloth1, 5, invalidatePendingCarry: false);
        _state.PendingItemColorCarryLayerIsCurrent(registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso,
            registration.Revisions).Should().BeTrue();

        _service.SetColor(Creature, Selection("a"), TintMapLayerType.Cloth1, new TintMapColor(1, 1, 1));
        _state.PendingItemColorCarryLayerIsCurrent(registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso,
            registration.Revisions).Should().BeFalse();
    }

    [Test]
    public void ResetColor_RemovesTheOverrideOrStoresAPresetBeneathAnActiveGlobalTint()
    {
        var selection = Selection("a");
        _world.SetInt(Item, Variable("a"), 40);

        _service.ResetColor(Creature, selection, TintMapLayerType.Cloth1);
        _world.LocalsOf(Item).Should().NotContainKey(Variable("a"));

        _world.SetInt(Item, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1),
            new TintMapColor(9, 9, 9).ToStoredValue());
        _service.ResetColor(Creature, selection, TintMapLayerType.Cloth1, paletteColor: 20);
        _world.Int(Item, Variable("a")).Should().Be(21, "the compatibility palette value is the colour plus one");

        _service.ResetColor(Creature, selection, TintMapLayerType.Cloth1);
        _world.Int(Item, Variable("a")).Should().Be(1, "the item's own standard colour is 0, plus one");
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(3);
    }

    [Test]
    public void ResetColorToInheritance_DeletesThePartOverrideSoTheGlobalTintShowsThrough()
    {
        _world.SetInt(Item, Variable("a"), 40);

        _service.ResetColorToInheritance(Creature, Selection("a"), TintMapLayerType.Cloth1);

        _world.LocalsOf(Item).Should().NotContainKey(Variable("a"));
        _apply.Calls.Should().Contain(call => call.Member == "ApplyColor");
    }

    [Test]
    public void SetGlobalItemCustomColor_RecordsTheTintAndColoursOnlyInheritingMaterials()
    {
        var color = new TintMapColor(70, 80, 90);
        var inheriting = Selection("inherits", AppearanceArmor.Torso);
        var preset = Selection("preset", AppearanceArmor.Belt);
        var custom = Selection("custom", AppearanceArmor.Pelvis);
        _world.SetInt(Item, Variable("preset"), 12);
        _world.SetInt(Item, Variable("custom"), new TintMapColor(1, 2, 3).ToStoredValue());

        _service.SetGlobalItemCustomColor(Creature, new[] { inheriting, preset, custom }, TintMapLayerType.Cloth1, color);

        _world.Int(Item, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1)).Should().Be(color.ToStoredValue());
        _world.Int(Item, TintMapVariable.GetItemGlobalInheritanceStateName(TintMapLayerType.Cloth1)).Should().Be(1);
        _apply.Calls.Where(call => call.Member == "ApplyColor").Should().ContainSingle()
            .Which.Arguments[3].Should().Be(new TintMapColorSelection(0, color));
        _droid.Calls.Should().ContainSingle(call => call.Member == "UpdateEquippedItemSnapshot");
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(1);
    }

    [Test]
    public void SetGlobalItemCustomColor_NormalisesALegacyUniformTintOnce()
    {
        var old = new TintMapColor(5, 5, 5);
        _world.SetInt(Item, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1), old.ToStoredValue());
        _world.SetInt(Item, Variable("a"), old.ToStoredValue());
        _world.SetInt(Item, Variable("b"), new TintMapColor(8, 8, 8).ToStoredValue());

        _service.SetGlobalItemCustomColor(Creature, new[] { Selection("a") }, TintMapLayerType.Cloth1, new TintMapColor(6, 6, 6));

        _world.LocalsOf(Item).Should().NotContainKey(Variable("a"), "a copy of the old global RGB inherits again");
        _world.LocalsOf(Item).Should().ContainKey(Variable("b"), "an independent colour stays");
    }

    [Test]
    public void SetGlobalItemCustomColor_InfersALegacyGlobalColorFromAUniformSetOfParts()
    {
        var old = new TintMapColor(5, 5, 5);
        _world.SetInt(Item, Variable("a"), old.ToStoredValue());
        _world.SetInt(Item, Variable("b"), old.ToStoredValue());

        _service.SetGlobalItemCustomColor(Creature,
            new[] { Selection("a", AppearanceArmor.Torso), Selection("b", AppearanceArmor.Belt) },
            TintMapLayerType.Cloth1, new TintMapColor(6, 6, 6));

        _world.LocalsOf(Item).Should().NotContainKey(Variable("a"));
        _world.LocalsOf(Item).Should().NotContainKey(Variable("b"));
    }

    [Test]
    public void SetGlobalItemCustomColor_ChoosesTheFirstItemWhenNoneIsGivenAndIgnoresCreatureLayers()
    {
        _service.SetGlobalItemCustomColor(Creature, new[] { Selection("a", layer: TintMapLayerType.Hair) },
            TintMapLayerType.Hair, new TintMapColor(1, 2, 3));
        _service.SetGlobalItemCustomColor(Creature, null!, TintMapLayerType.Cloth1, new TintMapColor(1, 2, 3));
        _service.SetGlobalItemCustomColor(Creature, Array.Empty<TintMapMaterialSelection>(), TintMapLayerType.Cloth1,
            new TintMapColor(1, 2, 3));
        _world.LocalsOf(Item).Should().BeEmpty();

        _service.SetGlobalItemCustomColor(Creature, new[] { Selection("a") }, TintMapLayerType.Cloth1, new TintMapColor(1, 2, 3));
        _world.LocalsOf(Item).Should().ContainKey(TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1));
    }

    [Test]
    public void ResetGlobalItemCustomColor_WithAnExplicitMarkerClearsItAndReappliesEachPart()
    {
        var global = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
        var marker = TintMapVariable.GetItemGlobalInheritanceStateName(TintMapLayerType.Cloth1);
        _world.SetInt(Item, global, new TintMapColor(9, 9, 9).ToStoredValue());
        _world.SetInt(Item, marker, 1);
        _world.SetInt(Item, Variable("a"), new TintMapColor(1, 2, 3).ToStoredValue());

        _service.ResetGlobalItemCustomColor(Creature, new[] { Selection("a") }, TintMapLayerType.Cloth1);

        _world.LocalsOf(Item).Should().NotContainKey(global).And.NotContainKey(marker).And.ContainKey(Variable("a"));
        Calls(_apply, "ApplyColor").Should().Be(1);
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(1);
    }

    [Test]
    public void ResetGlobalItemCustomColor_ForALegacyTintResetsTheMatchingPartsToTheirPresets()
    {
        var tint = new TintMapColor(9, 9, 9);
        _world.SetInt(Item, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1), tint.ToStoredValue());
        _world.SetInt(Item, Variable("a"), tint.ToStoredValue());
        _world.SetInt(Item, Variable("b"), new TintMapColor(1, 1, 1).ToStoredValue());

        _service.ResetGlobalItemCustomColor(Creature,
            new[] { Selection("a", AppearanceArmor.Torso), Selection("b", AppearanceArmor.Belt) }, TintMapLayerType.Cloth1);

        _world.LocalsOf(Item).Should().NotContainKey(Variable("a")).And.ContainKey(Variable("b"));
    }

    [Test]
    public void ResetGlobalItemCustomColor_IgnoresCreatureLayersAndSelectionsWithoutAnItem()
    {
        _service.ResetGlobalItemCustomColor(Creature, new[] { Selection("a", layer: TintMapLayerType.Hair) }, TintMapLayerType.Hair);
        _service.ResetGlobalItemCustomColor(Creature, null!, TintMapLayerType.Cloth1);
        _service.ResetGlobalItemCustomColor(Creature, Array.Empty<TintMapMaterialSelection>(), TintMapLayerType.Cloth1);

        _apply.Calls.Should().BeEmpty();
    }

    [Test]
    public void ResetInactiveItemCustomColor_WithoutAnArmorPartResetsTheGlobalTint()
    {
        var global = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
        _world.SetInt(Item, global, new TintMapColor(9, 9, 9).ToStoredValue());

        _service.ResetInactiveItemCustomColor(Creature, Item, TintMapLayerType.Cloth1, AppearanceArmor.Invalid);

        _world.LocalsOf(Item).Should().NotContainKey(global);
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(1);
    }

    [Test]
    public void ResetInactiveItemCustomColor_ForAPartClearsItsPerPartMarkerAndSnapshotsTheItem()
    {
        var marker = ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Torso, AppearanceArmorColor.Cloth1);
        _world.SetInt(Item, marker, 1);

        _service.ResetInactiveItemCustomColor(Creature, Item, TintMapLayerType.Cloth1, AppearanceArmor.Torso);

        _world.LocalsOf(Item).Should().NotContainKey(marker);
        _droid.Calls.Should().ContainSingle(call => call.Member == "UpdateEquippedItemSnapshot");
        Calls(_apply, "ApplyCurrentColorsAndPublish").Should().Be(1);
    }

    [Test]
    public void ResetInactiveItemCustomColor_IgnoresAnInvalidItemANonItemAndACreatureLayer()
    {
        _service.ResetInactiveItemCustomColor(Creature, 999, TintMapLayerType.Cloth1, AppearanceArmor.Torso);
        _service.ResetInactiveItemCustomColor(Creature, Creature, TintMapLayerType.Cloth1, AppearanceArmor.Torso);
        _service.ResetInactiveItemCustomColor(Creature, Item, TintMapLayerType.Skin, AppearanceArmor.Torso);

        _apply.Calls.Should().BeEmpty();
    }
}
