using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>The colour a creature, an item part or a worn material shows, from saved values and the appearance.</summary>
[TestFixture]
public class TintMapColorResolverTests
{
    private const uint Creature = 10;
    private const uint Item = 11;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature, player: true);
        _world.Add(Item, ObjectType.Item);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static TintMapMaterialSelection ItemSelection(
        TintMapLayerType layer = TintMapLayerType.Cloth1,
        AppearanceArmor armorPart = AppearanceArmor.Invalid,
        AppearanceWeapon weaponPart = AppearanceWeapon.Invalid,
        string material = "mat",
        string overrideModel = null!) =>
        new("model", new TintMapMaterialDefinition("Material", material, layer), Item, Creature, usesItemColors: true,
            armorPart, overrideModel, weaponPart);

    private static string Variable(string material, TintMapLayerType layer) => TintMapVariable.GetName(material, layer);

    [Test]
    public void GetEffectiveColor_UsesTheItemsAppearanceColorWhenNothingIsSaved()
    {
        _world.SetAppearance(Item, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 12);

        var color = TintMapColorResolver.GetEffectiveColor(Creature, ItemSelection(), TintMapLayerType.Cloth1);

        color.PaletteColorId.Should().Be(12);
        color.CustomColor.Should().BeNull();
    }

    [Test]
    public void GetEffectiveColor_ReadsALegacyPaletteIndexAndACustomColor()
    {
        var selection = ItemSelection();
        _world.SetInt(Item, Variable("mat", TintMapLayerType.Cloth1), 5);
        TintMapColorResolver.GetEffectiveColor(Creature, selection, TintMapLayerType.Cloth1)
            .Should().Be(new TintMapColorSelection(4, null));

        var custom = new TintMapColor(200, 10, 10);
        _world.SetInt(Item, Variable("mat", TintMapLayerType.Cloth1), custom.ToStoredValue());
        var color = TintMapColorResolver.GetEffectiveColor(Creature, selection, TintMapLayerType.Cloth1);

        color.CustomColor.Should().Be(custom);
        color.PaletteColorId.Should().Be(TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Cloth1, custom));
        TintMapColorResolver.GetEffectiveDisplayColor(Creature, selection, TintMapLayerType.Cloth1).Should().Be(custom);
        TintMapColorResolver.TryGetCustomColor(selection, TintMapLayerType.Cloth1, out var found).Should().BeTrue();
        found.Should().Be(custom);
    }

    [Test]
    public void GetEffectiveDisplayColor_ShowsThePaletteColorForAPresetSelection()
    {
        _world.SetAppearance(Item, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 3);

        TintMapColorResolver.GetEffectiveDisplayColor(Creature, ItemSelection(), TintMapLayerType.Cloth1)
            .Should().Be(TintMapPaletteColors.GetColor(TintMapLayerType.Cloth1, 3));
        TintMapColorResolver.TryGetCustomColor(ItemSelection(), TintMapLayerType.Cloth1, out _).Should().BeFalse();
    }

    [Test]
    public void GetEffectiveColor_ForACreatureLayerWithoutASavedValueUsesTheCreatureColor()
    {
        var selection = new TintMapMaterialSelection("model",
            new TintMapMaterialDefinition("Skin", "skinmat", TintMapLayerType.Skin), Creature, Creature,
            usesItemColors: false, AppearanceArmor.Invalid);
        _world.SetColor(Creature, ColorChannel.Skin, 7);

        TintMapColorResolver.GetEffectiveColor(Creature, selection, TintMapLayerType.Skin)
            .Should().Be(new TintMapColorSelection(7, null));

        var custom = new TintMapColor(40, 50, 60);
        _world.SetInt(Creature, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin), custom.ToStoredValue());
        var color = TintMapColorResolver.GetEffectiveCreatureColor(Creature, TintMapLayerType.Skin);

        color.CustomColor.Should().Be(custom);
        color.PaletteColorId.Should().Be(TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Skin, custom));
    }

    [Test]
    public void GetStandardColor_ReadsWeaponPartColorsAndClampsToThePalette()
    {
        _world.SetAppearance(Item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Top, 900);
        var selection = ItemSelection(weaponPart: AppearanceWeapon.Top);

        TintMapColorResolver.GetStandardColor(Creature, selection, TintMapLayerType.Metal1)
            .Should().Be(TintMapMaterialRegistry.PaletteColorCount - 1);
        TintMapColorResolver.GetStandardColorId(Creature, selection, TintMapLayerType.Metal1)
            .Should().Be(TintMapMaterialRegistry.PaletteColorCount - 1);
    }

    [Test]
    public void GetStandardColor_PrefersAPerPartArmorColorWhenItIsMarkedOrNonZero()
    {
        var perPart = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Torso, AppearanceArmorColor.Cloth1);
        var selection = ItemSelection(armorPart: AppearanceArmor.Torso);
        _world.SetAppearance(Item, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 21);

        // A zero per-part value inherits the item-wide color.
        TintMapColorResolver.GetStandardColor(Creature, selection, TintMapLayerType.Cloth1).Should().Be(21);

        _world.SetAppearance(Item, ItemAppearanceType.ArmorColor, perPart, 33);
        TintMapColorResolver.GetStandardColor(Creature, selection, TintMapLayerType.Cloth1).Should().Be(33);
    }

    [Test]
    public void GetStandardColor_ForARobeUsesTheNativePaletteBaseline()
    {
        var perPart = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Robe, AppearanceArmorColor.Cloth1);
        var selection = ItemSelection(armorPart: AppearanceArmor.Robe);
        _world.SetAppearance(Item, ItemAppearanceType.ArmorColor, perPart, 44);
        _world.SetInt(Item, TintMapNativePaletteProjection.BaselineName(perPart), 41);
        _world.SetInt(Item, TintMapNativePaletteProjection.LastAppliedName(perPart), 45);

        TintMapColorResolver.GetStandardColor(Creature, selection, TintMapLayerType.Cloth1).Should().Be(40);
    }

    [Test]
    public void GetSavedColor_FallsBackToTheItemWideColorUnlessAPresetIsExplicit()
    {
        var selection = ItemSelection();
        var global = new TintMapColor(5, 6, 7).ToStoredValue();
        _world.SetInt(Item, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1), global);

        TintMapColorResolver.GetSavedColor(selection, TintMapLayerType.Cloth1).Should().Be(global);
        TintMapColorResolver.GetSavedColor(selection, TintMapLayerType.Cloth1, includeGlobalColor: false).Should().Be(0);

        _world.SetInt(Item, Variable("mat", TintMapLayerType.Cloth1), 9);
        TintMapColorResolver.GetSavedColor(selection, TintMapLayerType.Cloth1).Should().Be(9);
        TintMapColorResolver.HasExplicitItemPresetColor(selection, TintMapLayerType.Cloth1, 9).Should().BeTrue();
    }

    [Test]
    public void HasExplicitItemPresetColor_RecognisesAPerPartOverrideMarker()
    {
        var selection = ItemSelection(armorPart: AppearanceArmor.Torso);
        TintMapColorResolver.HasExplicitItemPresetColor(selection, TintMapLayerType.Cloth1, 0).Should().BeFalse();

        _world.SetInt(Item, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Torso,
            AppearanceArmorColor.Cloth1), 1);

        TintMapColorResolver.HasExplicitItemPresetColor(selection, TintMapLayerType.Cloth1, 0).Should().BeTrue();
        TintMapColorResolver.HasExplicitItemPresetColor(ItemSelection(), TintMapLayerType.Cloth1, 0).Should().BeFalse();
    }

    [Test]
    public void GetSavedColor_ReadsTheWornCloakMaterialsColorForADroppedCloak()
    {
        var state = _container.Get<ITintMapStateService>();
        state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>
        {
            ["worn_cloak"] = new() { new TintMapMaterialDefinition("Worn", "wornmat", TintMapLayerType.Cloth1) }
        });
        var selection = ItemSelection(overrideModel: "worn_cloak");
        _world.SetInt(Item, Variable("wornmat", TintMapLayerType.Cloth1), 17);

        TintMapColorResolver.GetSavedColor(selection, TintMapLayerType.Cloth1).Should().Be(17);
    }

    [Test]
    public void GetSavedColor_ForAWornCloakFindsTheOneEquivalentStoredColor()
    {
        var state = _container.Get<ITintMapStateService>();
        state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>
        {
            ["worn_cloak"] = new() { new TintMapMaterialDefinition("Worn", "wornmat", TintMapLayerType.Cloth1) }
        });
        var selection = ItemSelection(overrideModel: "worn_cloak");

        // Nothing stored under a material of the worn model, and nothing equivalent stored either.
        TintMapColorResolver.GetSavedColor(selection, TintMapLayerType.Cloth1, includeGlobalColor: false).Should().Be(0);
    }

    [Test]
    public void LayerChannelMappingsAreTheCreatureAndArmorChannels()
    {
        TintMapColorResolver.GetCreatureColorChannel(TintMapLayerType.Skin).Should().Be(ColorChannel.Skin);
        TintMapColorResolver.GetCreatureColorChannel(TintMapLayerType.Hair).Should().Be(ColorChannel.Hair);
        TintMapColorResolver.GetCreatureColorChannel(TintMapLayerType.Tattoo1).Should().Be(ColorChannel.Tattoo1);
        TintMapColorResolver.GetCreatureColorChannel(TintMapLayerType.Tattoo2).Should().Be(ColorChannel.Tattoo2);
        ((int)TintMapColorResolver.GetCreatureColorChannel(TintMapLayerType.Metal1)).Should().BeNegative();

        TintMapColorResolver.TryGetArmorColorChannel(TintMapLayerType.Leather2, out var channel).Should().BeTrue();
        channel.Should().Be(AppearanceArmorColor.Leather2);
        TintMapColorResolver.TryGetArmorColorChannel(TintMapLayerType.Skin, out _).Should().BeFalse();
    }

    [Test]
    public void CreatureVariables_ListOnlyTheCreatureLayerLocals()
    {
        var hair = Variable("mat", TintMapLayerType.Hair);
        var hairState = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair);
        _world.SetInt(Creature, hair, 3);
        _world.SetInt(Creature, Variable("mat", TintMapLayerType.Cloth1), 4);
        _world.SetInt(Creature, "UNRELATED", 5);

        TintMapColorResolver.GetCreatureCustomColorVariables(Creature).Should().BeEquivalentTo(new[] { hair });
        TintMapColorResolver.GetCreatureTintSnapshotVariables(Creature).Should().Contain(hair).And.Contain(hairState)
            .And.HaveCount(1 + 4);
        TintMapColorResolver.GetCreatureCustomColorStateVariable(TintMapLayerType.Hair).Should().Be(hairState);
    }

    [Test]
    public void GetEquivalentItemTintVariables_ReturnsNothingForACreatureLayerOrAnInvalidItem()
    {
        var selection = ItemSelection(TintMapLayerType.Hair);

        TintMapColorResolver.GetEquivalentItemTintVariables(Item, selection, TintMapLayerType.Hair).Should().BeEmpty();
        TintMapColorResolver.GetEquivalentItemTintVariables(999, ItemSelection(), TintMapLayerType.Cloth1).Should().BeEmpty();
        TintMapColorResolver.GetEquivalentItemTintVariables(Creature, ItemSelection(), TintMapLayerType.Cloth1).Should().BeEmpty();
    }

    [Test]
    public void GetEquivalentItemTintVariables_AddsEveryRenderProfileOfARobe()
    {
        var state = _container.Get<ITintMapStateService>();
        state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>
        {
            ["robe_model"] = new()
            {
                new TintMapMaterialDefinition("A", "robe_a", TintMapLayerType.Cloth1),
                new TintMapMaterialDefinition("B", "robe_b", TintMapLayerType.Cloth1, TintMapLayerType.Metal1)
            }
        });
        var selection = new TintMapMaterialSelection("robe_model",
            new TintMapMaterialDefinition("A", "robe_a", TintMapLayerType.Cloth1), Item, Creature, true, AppearanceArmor.Robe);

        TintMapColorResolver.GetEquivalentItemTintVariables(Item, selection, TintMapLayerType.Cloth1)
            .Should().BeEquivalentTo(new[] { Variable("robe_a", TintMapLayerType.Cloth1), Variable("robe_b", TintMapLayerType.Cloth1) });
    }
}
