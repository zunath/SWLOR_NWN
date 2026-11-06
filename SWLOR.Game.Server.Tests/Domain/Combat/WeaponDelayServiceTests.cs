using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>The weapon delay instance: the category table, the Delay property checks and the equip fallback.</summary>
[TestFixture]
public class WeaponDelayServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Weapon = 3001;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private MappingOnlyItemService _items = null!;
    private WeaponDelayService _delay = null!;
    private BaseItem _baseItem;
    private ItemPropertyType[] _properties = Array.Empty<ItemPropertyType>();
    private int _cursor;

    [SetUp]
    public void SetUp()
    {
        _properties = Array.Empty<ItemPropertyType>();
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetBaseItemType", _ => _baseItem);
        var property = EngineStructures.Create<ItemProperty>(1);
        _engine.SetReturnHandler("GetFirstItemProperty", _ =>
        {
            _cursor = 0;
            return property;
        });
        _engine.SetReturnHandler("GetNextItemProperty", _ =>
        {
            _cursor++;
            return property;
        });
        _engine.SetReturnHandler("GetIsItemPropertyValid", _ => _cursor < _properties.Length);
        _engine.SetReturnHandler("GetItemPropertyType", _ => _cursor < _properties.Length ? _properties[_cursor] : ItemPropertyType.Invalid);
        _scope = NWScriptRuntime.Override(_engine);

        _items = new MappingOnlyItemService(null!, new InMemoryDBService());
        _delay = new WeaponDelayService(_items);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [TestCase("Vibroblade", ItemPropertyAttackDelay.Delay230)]
    [TestCase("Katar", ItemPropertyAttackDelay.Delay220)]
    [TestCase("PowerGlove", ItemPropertyAttackDelay.Delay220)]
    [TestCase("TwinBlade", ItemPropertyAttackDelay.Delay230)]
    [TestCase("Vibroknife", ItemPropertyAttackDelay.Delay220)]
    [TestCase("Staff", ItemPropertyAttackDelay.Delay270)]
    [TestCase("Rifle", ItemPropertyAttackDelay.Delay300)]
    [TestCase("HeavyVibroblade", ItemPropertyAttackDelay.Delay300)]
    [TestCase("Pistol", ItemPropertyAttackDelay.Delay250)]
    [TestCase("Lightsaber", ItemPropertyAttackDelay.Delay240)]
    [TestCase("Spear", ItemPropertyAttackDelay.Delay280)]
    [TestCase("ThrowingWeapon", ItemPropertyAttackDelay.Delay220)]
    [TestCase("Saberstaff", ItemPropertyAttackDelay.Delay240)]
    [TestCase("Creature", ItemPropertyAttackDelay.Delay240)]
    public void GetWeaponDelay_AnswersTheDelayOfTheWeaponCategory(string category, ItemPropertyAttackDelay expected)
    {
        var list = (List<BaseItem>)typeof(IItemService).GetProperty(category + "BaseItemTypes")!.GetValue(_items)!;

        foreach (var baseItem in list)
            _delay.GetWeaponDelay(baseItem).Should().Be(expected, baseItem.ToString());
    }

    [Test]
    public void GetWeaponDelay_AnswersNullForAnItemThatIsNoWeapon()
    {
        _delay.GetWeaponDelay((BaseItem)(-12345)).Should().BeNull();
    }

    [Test]
    public void TheCategoryTableIsBuiltOnceAndReadsTheItemServicesLists()
    {
        var rifle = _items.RifleBaseItemTypes[0];
        _delay.GetWeaponDelay(rifle).Should().Be(ItemPropertyAttackDelay.Delay300);

        _items.RifleBaseItemTypes.Add((BaseItem)(-777));

        _delay.GetWeaponDelay((BaseItem)(-777)).Should().Be(ItemPropertyAttackDelay.Delay300);
    }

    [Test]
    public void IsWeapon_ReadsTheBaseItemOfTheItem()
    {
        _baseItem = _items.PistolBaseItemTypes[0];
        _delay.IsWeapon(Weapon).Should().BeTrue();

        _baseItem = (BaseItem)(-12345);
        _delay.IsWeapon(Weapon).Should().BeFalse();
    }

    [Test]
    public void HasDelayProperty_FindsADelayPropertyAmongTheItemsProperties()
    {
        _properties = new[] { ItemPropertyType.Invalid, ItemPropertyType.Delay };
        _delay.HasDelayProperty(Weapon).Should().BeTrue();

        _properties = new[] { ItemPropertyType.Invalid };
        _delay.HasDelayProperty(Weapon).Should().BeFalse();

        _properties = Array.Empty<ItemPropertyType>();
        _delay.HasDelayProperty(Weapon).Should().BeFalse();
    }

    [Test]
    public void AddDelayProperty_AddsAPermanentDelayPropertyWithTheValue()
    {
        _delay.AddDelayProperty(Weapon, ItemPropertyAttackDelay.Delay250);

        var custom = _engine.CallsTo("ItemPropertyCustom").Should().ContainSingle().Subject;
        custom.Arguments[0].Should().Be(ItemPropertyType.Delay);
        custom.Arguments[1].Should().Be(-1);
        custom.Arguments[2].Should().Be((int)ItemPropertyAttackDelay.Delay250);
        _engine.CallsTo("AddItemProperty").Should().ContainSingle();
    }

    [TestCase(ItemPropertyAttackDelay.Invalid)]
    [TestCase((ItemPropertyAttackDelay)99999)]
    public void AddDelayProperty_RefusesAnInvalidDelay(ItemPropertyAttackDelay delay)
    {
        var add = () => _delay.AddDelayProperty(Weapon, delay);

        add.Should().Throw<ArgumentOutOfRangeException>();
        _engine.CountOf("AddItemProperty").Should().Be(0);
    }

    [Test]
    public void ApplyDelayPropertyIfMissing_AddsTheCategoryDelayToAWeaponWithoutOne()
    {
        _baseItem = _items.SpearBaseItemTypes[0];

        _delay.ApplyDelayPropertyIfMissing(Weapon);

        _engine.CallsTo("ItemPropertyCustom").Should().ContainSingle()
            .Which.Arguments[2].Should().Be((int)ItemPropertyAttackDelay.Delay280);
        _engine.CountOf("AddItemProperty").Should().Be(1);
    }

    [Test]
    public void ApplyDelayPropertyIfMissing_LeavesAWeaponThatHasOneAndItemsThatAreNoWeapons()
    {
        _baseItem = _items.SpearBaseItemTypes[0];
        _properties = new[] { ItemPropertyType.Delay };
        _delay.ApplyDelayPropertyIfMissing(Weapon);

        _baseItem = (BaseItem)(-12345);
        _properties = Array.Empty<ItemPropertyType>();
        _delay.ApplyDelayPropertyIfMissing(Weapon);

        _engine.CountOf("AddItemProperty").Should().Be(0);
    }

    [Test]
    public void ApplyWeaponDelayFallback_ProcessesTheLastEquippedItemWhenItIsValid()
    {
        _baseItem = _items.RifleBaseItemTypes[0];
        _engine.SetReturn("GetPCItemLastEquipped", Weapon);

        _delay.ApplyWeaponDelayFallback();

        _engine.CallsTo("ItemPropertyCustom").Should().ContainSingle()
            .Which.Arguments[2].Should().Be((int)ItemPropertyAttackDelay.Delay300);
    }

    [Test]
    public void ApplyWeaponDelayFallback_IgnoresAnInvalidItem()
    {
        _engine.SetReturn("GetPCItemLastEquipped", ObjectInvalid);

        _delay.ApplyWeaponDelayFallback();

        _engine.CountOf("GetBaseItemType").Should().Be(0);
    }
}
