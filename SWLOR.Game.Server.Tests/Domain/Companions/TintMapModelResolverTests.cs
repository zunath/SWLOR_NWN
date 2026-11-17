using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>Which materials a creature, its worn gear and a dropped item show, read from the 2DAs and the material table.</summary>
[TestFixture]
public class TintMapModelResolverTests
{
    private const uint Creature = 10;
    private const uint Armor = 11;
    private const uint Helmet = 12;
    private const uint Cloak = 13;
    private const uint Dropped = 14;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature);
        _world.Set2DA("appearance", "MODELTYPE", 0, "P");
        _world.Set2DA("appearance", "RACE", 0, "H");
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private readonly Dictionary<string, List<TintMapMaterialDefinition>> Registered = new();

    private void Register(string model, string material, params TintMapLayerType[] layers)
    {
        Registered[model] = new() { new TintMapMaterialDefinition("Material", material, layers) };
        _container.Get<ITintMapStateService>().LoadMaterials(Registered);
    }

    [Test]
    public void GetCurrentSelections_ForANakedPartsBodyUsesTheCreaturesOwnParts()
    {
        _world.SetBodyPart(Creature, CreaturePart.Torso, 5);
        _world.SetBodyPart(Creature, CreaturePart.Head, 3);
        Register("pmh0_chest005", "pmh0_chest005", TintMapLayerType.Skin);
        Register("pmh0_head003", "pmh0_head003", TintMapLayerType.Hair);

        var selections = TintMapModelResolver.GetCurrentSelections(Creature);

        selections.Select(selection => selection.Material.Resref).Should().BeEquivalentTo("pmh0_chest005", "pmh0_head003");
        selections.Should().OnlyContain(selection => selection.PaletteSource == Creature && !selection.UsesItemColors
            && selection.ArmorPart == AppearanceArmor.Invalid);
    }

    [Test]
    public void GetCurrentSelections_ForWornArmorTakesTheArmorPartsRobeHelmetAndCloak()
    {
        _world.Add(Armor, ObjectType.Item);
        _world.Add(Helmet, ObjectType.Item);
        _world.Add(Cloak, ObjectType.Item);
        _world.Equip(Creature, InventorySlot.Chest, Armor);
        _world.Equip(Creature, InventorySlot.Head, Helmet);
        _world.Equip(Creature, InventorySlot.Cloak, Cloak);
        _world.SetBodyPart(Creature, CreaturePart.Torso, 5);
        _world.SetBodyPart(Creature, CreaturePart.Head, 3);
        _world.SetAppearance(Armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 9);
        _world.SetAppearance(Armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 4);
        _world.SetAppearance(Helmet, ItemAppearanceType.SimpleModel, 0, 7);
        _world.SetAppearance(Cloak, ItemAppearanceType.SimpleModel, 0, 2);
        _world.Set2DA("cloakmodel", "TEXTURE", 2, "6");
        Register("pmh0_chest009", "pmh0_chest009", TintMapLayerType.Cloth1);
        Register("pmh0_head003", "pmh0_head003", TintMapLayerType.Hair);
        Register("pmh0_robe004", "pmh0_robe004", TintMapLayerType.Cloth2);
        Register("helm_007", "helm_007", TintMapLayerType.Metal1);
        Register("cloak_006", "cloak_006", TintMapLayerType.Leather1);

        var selections = TintMapModelResolver.GetCurrentSelections(Creature);

        var torso = selections.Single(selection => selection.Material.Resref == "pmh0_chest009");
        torso.PaletteSource.Should().Be(Armor);
        torso.ArmorPart.Should().Be(AppearanceArmor.Torso);
        selections.Single(selection => selection.Material.Resref == "pmh0_head003").PaletteSource.Should().Be(Armor,
            "a head borrows the armor's dyes but keeps the creature's head");
        selections.Single(selection => selection.Material.Resref == "pmh0_robe004").ArmorPart.Should().Be(AppearanceArmor.Robe);
        selections.Single(selection => selection.Material.Resref == "helm_007").PaletteSource.Should().Be(Helmet);
        selections.Single(selection => selection.Material.Resref == "cloak_006").PaletteSource.Should().Be(Cloak);
    }

    [Test]
    public void GetCurrentSelections_ForACloakWithoutARaceSpecificModelFallsBackToTheGenericOne()
    {
        _world.Add(Cloak, ObjectType.Item);
        _world.Equip(Creature, InventorySlot.Cloak, Cloak);
        _world.SetAppearance(Cloak, ItemAppearanceType.SimpleModel, 0, 2);
        Register("cloak_002", "cloak_002", TintMapLayerType.Leather1);

        TintMapModelResolver.GetCurrentSelections(Creature).Should().ContainSingle()
            .Which.ModelResref.Should().Be("cloak_002");
    }

    [Test]
    public void GetCurrentSelections_ForAModelTableAppearanceReadsItsModelAndSimpleGear()
    {
        _world.Set2DA("appearance", "MODELTYPE", 0, "S");
        _world.Set2DA("appearance", "RACE", 0, "droidmodel");
        _world.Add(Helmet, ObjectType.Item);
        _world.Equip(Creature, InventorySlot.Head, Helmet);
        _world.SetAppearance(Helmet, ItemAppearanceType.SimpleModel, 0, 7);
        Register("droidmodel", "droid_mat", TintMapLayerType.Metal1);
        Register("helm_007", "helm_007", TintMapLayerType.Metal2);

        var selections = TintMapModelResolver.GetCurrentSelections(Creature);

        selections.Select(selection => selection.Material.Resref).Should().BeEquivalentTo("droid_mat", "helm_007");
        selections.Single(selection => selection.Material.Resref == "helm_007").IsWornHelmet.Should().BeTrue();
    }

    [Test]
    public void GetCurrentSelections_WithoutAKnownRaceListsNoBodyParts()
    {
        _world.Set2DA("appearance", "RACE", 0, "****");
        _world.SetBodyPart(Creature, CreaturePart.Torso, 5);
        Register("pmh0_chest005", "pmh0_chest005", TintMapLayerType.Skin);

        TintMapModelResolver.GetCurrentSelections(Creature).Should().BeEmpty();
    }

    [Test]
    public void GetCurrentRobeModelResref_NamesTheRobeOfAPartsBodyOnly()
    {
        TintMapModelResolver.GetCurrentRobeModelResref(Creature, 4).Should().Be("pmh0_robe004");

        _world.Set2DA("appearance", "MODELTYPE", 0, "S");
        TintMapModelResolver.GetCurrentRobeModelResref(Creature, 4).Should().BeEmpty();

        _world.Set2DA("appearance", "MODELTYPE", 0, "P");
        _world.Set2DA("appearance", "RACE", 0, "");
        TintMapModelResolver.GetCurrentRobeModelResref(Creature, 4).Should().BeEmpty();
    }

    [Test]
    public void GetWorldItemSelections_ReadsASimpleModelAndAWeaponsThreeParts()
    {
        _world.Add(Dropped, ObjectType.Item);
        _world.SetBaseItem(Dropped, BaseItem.Cloak);
        _world.Set2DA("baseitems", "ItemClass", (int)BaseItem.Cloak, "CLOAK");
        _world.Set2DA("baseitems", "ModelType", (int)BaseItem.Cloak, "0");
        _world.SetAppearance(Dropped, ItemAppearanceType.SimpleModel, 0, 2);
        _world.Set2DA("cloakmodel", "TEXTURE", 2, "6");
        Register("cloak_002", "ground_cloak", TintMapLayerType.Leather1);

        var cloak = TintMapModelResolver.GetWorldItemSelections(Dropped).Should().ContainSingle().Subject;
        cloak.OverrideModelResref.Should().Be("cloak_006");
        cloak.PaletteSource.Should().Be(Dropped);

        _world.SetBaseItem(Dropped, BaseItem.Longsword);
        _world.Set2DA("baseitems", "ItemClass", (int)BaseItem.Longsword, "wswls");
        _world.Set2DA("baseitems", "ModelType", (int)BaseItem.Longsword, "2");
        _world.SetAppearance(Dropped, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Top, 3);
        Register("wswls_t_003", "blade_mat", TintMapLayerType.Metal1);

        TintMapModelResolver.GetWorldItemSelections(Dropped).Should().ContainSingle()
            .Which.WeaponPart.Should().Be(AppearanceWeapon.Top);
    }

    [Test]
    public void GetWorldItemSelections_IgnoresAnItemWithoutAClassOrAModelType()
    {
        _world.Add(Dropped, ObjectType.Item);
        _world.SetBaseItem(Dropped, BaseItem.Cloak);

        TintMapModelResolver.GetWorldItemSelections(Dropped).Should().BeEmpty();
        TintMapModelResolver.GetWorldItemSelections(999).Should().BeEmpty();

        _world.Set2DA("baseitems", "ItemClass", (int)BaseItem.Cloak, "cloak");
        _world.Set2DA("baseitems", "ModelType", (int)BaseItem.Cloak, "x");
        TintMapModelResolver.GetWorldItemSelections(Dropped).Should().BeEmpty();
    }

    [Test]
    public void CopyArmorPartTintOverrides_MirrorsAPartsColorsOntoTheOppositePartAndClearsTheOldOnes()
    {
        _world.Add(Armor, ObjectType.Item);
        _world.SetBodyPart(Creature, CreaturePart.LeftForearm, 10);
        _world.SetAppearance(Armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm, 10);
        _world.SetAppearance(Armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm, 8);
        Register("pmh0_forel010", "pmh0_forel010", TintMapLayerType.Cloth1);
        Register("pmh0_forer010", "pmh0_forer010", TintMapLayerType.Cloth1);
        Register("pmh0_forer008", "pmh0_forer008", TintMapLayerType.Cloth1);
        var source = TintMapVariable.GetName("pmh0_forel010", TintMapLayerType.Cloth1);
        var destination = TintMapVariable.GetName("pmh0_forer010", TintMapLayerType.Cloth1);
        var previous = TintMapVariable.GetName("pmh0_forer008", TintMapLayerType.Cloth1);
        _world.SetInt(Armor, source, 77);
        _world.SetInt(Armor, previous, 55);

        TintMapModelResolver.CopyArmorPartTintOverrides(Creature, Armor, AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm);

        _world.Int(Armor, destination).Should().Be(77);
        _world.LocalsOf(Armor).Should().NotContainKey(previous, "the replaced destination model no longer shows that material");
    }

    [Test]
    public void CopyArmorPartTintOverrides_DoesNothingForAnInvalidItemAPartWithoutAModelOrANonBodyPart()
    {
        _world.Add(Armor, ObjectType.Item);
        _world.SetInt(Armor, "TM_keep_0", 5);

        TintMapModelResolver.CopyArmorPartTintOverrides(Creature, 999, AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm);
        TintMapModelResolver.CopyArmorPartTintOverrides(Creature, Armor, AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm);
        TintMapModelResolver.CopyArmorPartTintOverrides(Creature, Armor, AppearanceArmor.Robe, AppearanceArmor.RightForearm);

        _world.LocalsOf(Armor).Should().BeEquivalentTo(new Dictionary<string, int> { ["TM_keep_0"] = 5 });
    }
}
