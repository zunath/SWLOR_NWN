using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>Unit tests of the enhancement item properties: the engine is a recording fake and the resistance service reaches the installed container.</summary>
[TestFixture]
public class CraftEnhancementPropertyServiceTests
{
    private const uint PlayerObject = 1001;

    private CraftEnhancementPropertyService _service = null!;
    private PlayerEngine _engine = null!;
    private FakeEventsPlugin _events = null!;
    private FakeItemPropertyPlugin _itemProperties = null!;
    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;

    [OneTimeSetUp]
    public void LoadTheSkills()
    {
        // The resistance encoding is reached through the Resistance facade, which needs a container.
        _facades = new FacadeContainer();
        using (_facades.Install())
        using (new PlayerEngine())
        using (EventsPluginRuntime.Override(new FakeEventsPlugin()))
        {
            Skill.CacheData();
            Skill.CacheXPChartData();
        }
    }

    [OneTimeTearDown]
    public void DisposeTheFacades() => _facades.Dispose();

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        _engine.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _engine = new PlayerEngine();
        _events = new FakeEventsPlugin();
        _itemProperties = new FakeItemPropertyPlugin();
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _scopes.Add(ItemPropertyPluginRuntime.Override(_itemProperties));
        _service = new CraftEnhancementPropertyService(FacadeForwarder.For<IResistanceService>());
    }

    // ---- enhancement item properties

    [Test]
    public void BuildItemPropertyForEnhancement_builds_the_property_of_each_supported_sub_type()
    {
        var supported = 0;
        foreach (var subType in Enum.GetValues<EnhancementSubType>().Where(value => value != EnhancementSubType.Invalid))
        {
            _engine.Engine.ClearCalls();
            try
            {
                _service.BuildItemPropertyForEnhancement(subType, 3);
            }
            catch (Exception exception) when (exception.Message == "Unsupported enhancement type.")
            {
                continue;
            }

            supported++;
            _engine.Engine.Calls.Should().Contain(call => call.Member.StartsWith("ItemProperty"), $"{subType} builds a property");
        }

        supported.Should().BeGreaterThan(30);
    }

    [Test]
    public void BuildItemPropertyForEnhancement_rejects_the_invalid_sub_type()
    {
        var build = () => _service.BuildItemPropertyForEnhancement(EnhancementSubType.Invalid, 1);

        build.Should().Throw<Exception>().WithMessage("Unsupported enhancement type.");
    }

    [Test]
    public void BuildItemPropertyForEnhancement_defense_uses_the_damage_type_as_the_sub_type()
    {
        _service.BuildItemPropertyForEnhancement(EnhancementSubType.DefenseForce, 4);

        var call = _engine.Engine.CallsTo("ItemPropertyCustom").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(ItemPropertyType.Defense);
        call.Arguments[1].Should().Be((int)CombatDamageType.Force);
        call.Arguments[2].Should().Be(4);
    }

    [Test]
    public void BuildItemPropertyForEnhancement_resistance_encodes_the_amount()
    {
        _service.BuildItemPropertyForEnhancement(EnhancementSubType.ResistanceFire, 5);

        var call = _engine.Engine.CallsTo("ItemPropertyCustom").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(ItemPropertyType.Resistance);
        call.Arguments[1].Should().Be((int)ResistanceType.Fire);
        call.Arguments[2].Should().Be(Resistance.EncodeItemPropertyCostTableValue(5));
    }

    [Test]
    public void BuildItemPropertiesForEnhancement_adds_a_damage_type_property_for_a_non_physical_weapon_damage_type()
    {
        var properties = _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2, CombatDamageType.Fire).ToList();

        properties.Should().HaveCount(2);
        var calls = _engine.Engine.CallsTo("ItemPropertyCustom");
        calls[^1].Arguments[0].Should().Be(ItemPropertyType.WeaponDamageType);
        calls[^1].Arguments[1].Should().Be((int)CombatDamageType.Fire);
    }

    [Test]
    public void BuildItemPropertiesForEnhancement_adds_nothing_extra_for_physical_damage_or_another_sub_type()
    {
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2, CombatDamageType.Physical).Should().HaveCount(1);
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.HP, 2, CombatDamageType.Fire).Should().HaveCount(1);
        _service.BuildItemPropertiesForEnhancement(EnhancementSubType.DMG, 2).Should().HaveCount(1);
    }

    [Test]
    public void IsWeaponDamageEnhancement_is_true_only_for_DMG()
    {
        foreach (var subType in Enum.GetValues<EnhancementSubType>())
            _service.IsWeaponDamageEnhancement(subType).Should().Be(subType == EnhancementSubType.DMG);
    }

    [TestCase(EnhancementSubType.HP, CombatDamageType.Fire, false, CombatDamageType.Invalid)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Force, true, CombatDamageType.Physical)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Fire, true, CombatDamageType.Fire)]
    [TestCase(EnhancementSubType.DMG, CombatDamageType.Invalid, true, CombatDamageType.Physical)]
    public void TryGetWeaponDamageTypeForEnhancement_resolves_the_damage_type(
        EnhancementSubType subType,
        CombatDamageType explicitType,
        bool expectedResult,
        CombatDamageType expectedType)
    {
        _service.TryGetWeaponDamageTypeForEnhancement(subType, explicitType, out var resolved).Should().Be(expectedResult);

        resolved.Should().Be(expectedType);
    }

    // ---- merging crafted properties

    [Test]
    public void MergeCraftedItemProperty_adds_a_property_of_a_new_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void MergeCraftedItemProperty_sums_properties_of_the_same_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        var properties = new List<ItemProperty> { EngineStructures.Create<ItemProperty>() };

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void MergeCraftedItemProperty_never_keeps_a_force_weapon_damage_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Force);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().BeEmpty();
    }

    [Test]
    public void MergeCraftedItemProperty_replaces_the_weapon_damage_type_when_the_roll_does_not_keep_the_old_one()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Fire);
        var properties = new List<ItemProperty>();

        _service.MergeCraftedItemProperty(properties, EngineStructures.Create<ItemProperty>());

        properties.Should().HaveCount(1);
    }

    [Test]
    public void ApplyCraftedItemProperty_ignores_a_force_weapon_damage_type()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.WeaponDamageType);
        _engine.Engine.SetReturn("GetItemPropertySubType", (int)CombatDamageType.Force);

        _service.ApplyCraftedItemProperty(PlayerObject, EngineStructures.Create<ItemProperty>());

        _engine.Engine.CountOf("RemoveItemProperty").Should().Be(0);
    }

    [Test]
    public void ApplyCraftedItemProperty_removes_the_matching_properties_before_adding_the_summed_one()
    {
        _engine.Engine.SetReturn("GetItemPropertyType", ItemPropertyType.HPBonus);
        _engine.Engine.SetReturn("GetItemPropertySubType", -1);
        _engine.Engine.SetReturn("GetItemPropertyCostTableValue", 3);
        _engine.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);

        _service.ApplyCraftedItemProperty(PlayerObject, EngineStructures.Create<ItemProperty>());

        _engine.Engine.CountOf("RemoveItemProperty").Should().Be(2);
    }

}
