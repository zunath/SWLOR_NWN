using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>What a creature holds in each hand decides which weapon predicates are true, from the base item type of the equipped items.</summary>
[TestFixture]
public class EquipmentPredicatesTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Creature = 1001;
    private const uint RightItem = 2001;
    private const uint LeftItem = 2002;

    private FacadeContainer _container = null!;
    private IDisposable _facades = null!;
    private IDisposable _engineScope = null!;
    private readonly Dictionary<InventorySlot, uint> _slots = new();
    private readonly Dictionary<uint, BaseItem> _types = new();

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp()
    {
        _slots.Clear();
        _types.Clear();
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        engine.SetReturnHandler("GetItemInSlot", args => _slots.GetValueOrDefault((InventorySlot)args[0]!, Invalid));
        engine.SetReturnHandler("GetBaseItemType", args => _types.GetValueOrDefault((uint)args[0]!, BaseItem.Invalid));
        _engineScope = NWScriptRuntime.Override(engine);
        _facades = _container.Install();
    }

    [TearDown]
    public void TearDown()
    {
        _facades.Dispose();
        _engineScope.Dispose();
    }

    private void Right(BaseItem type)
    {
        _slots[InventorySlot.RightHand] = RightItem;
        _types[RightItem] = type;
    }

    private void Left(BaseItem type)
    {
        _slots[InventorySlot.LeftHand] = LeftItem;
        _types[LeftItem] = type;
    }

    [Test]
    public void ABareHandedCreatureMatchesNothing()
    {
        EquipmentPredicates.HasSingleWeapon(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandLightsaber(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandStaff(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandSpear(Creature).Should().BeFalse();
        EquipmentPredicates.HasPistol(Creature).Should().BeFalse();
        EquipmentPredicates.HasTwinPistol(Creature).Should().BeFalse();
        EquipmentPredicates.HasRifle(Creature).Should().BeFalse();
        EquipmentPredicates.HasThrowing(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandKatar(Creature).Should().BeFalse();
        EquipmentPredicates.HasDualWieldKatar(Creature).Should().BeFalse();
        EquipmentPredicates.HasDualWield(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandVibroknife(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandVibroblade(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandSaberstaff(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandTwinBlade(Creature).Should().BeFalse();
        EquipmentPredicates.HasMainHandHeavyVibroblade(Creature).Should().BeFalse();
        EquipmentPredicates.HasOffHandShield(Creature).Should().BeFalse();
    }

    [TestCase(BaseItem.Lightsaber, nameof(EquipmentPredicates.HasMainHandLightsaber))]
    [TestCase(BaseItem.QuarterStaff, nameof(EquipmentPredicates.HasMainHandStaff))]
    [TestCase(BaseItem.Halberd, nameof(EquipmentPredicates.HasMainHandSpear))]
    [TestCase(BaseItem.Longbow, nameof(EquipmentPredicates.HasRifle))]
    [TestCase(BaseItem.ThrowingAxe, nameof(EquipmentPredicates.HasThrowing))]
    [TestCase(BaseItem.Katar, nameof(EquipmentPredicates.HasMainHandKatar))]
    [TestCase(BaseItem.Dagger, nameof(EquipmentPredicates.HasMainHandVibroknife))]
    [TestCase(BaseItem.BastardSword, nameof(EquipmentPredicates.HasMainHandVibroblade))]
    [TestCase(BaseItem.Saberstaff, nameof(EquipmentPredicates.HasMainHandSaberstaff))]
    [TestCase(BaseItem.DoubleAxe, nameof(EquipmentPredicates.HasMainHandTwinBlade))]
    [TestCase(BaseItem.DireMace, nameof(EquipmentPredicates.HasMainHandHeavyVibroblade))]
    [TestCase(BaseItem.Pistol, nameof(EquipmentPredicates.HasPistol))]
    public void AMainHandItemOfTheTypeMatchesItsPredicateOnly(BaseItem type, string predicate)
    {
        Right(type);
        var predicates = typeof(EquipmentPredicates).GetMethods().Where(m =>
            m.GetParameters().Length == 1 && m.ReturnType == typeof(bool) && m.Name.StartsWith("Has") &&
            m.GetParameters()[0].ParameterType == typeof(uint));

        foreach (var method in predicates)
        {
            var result = (bool)method.Invoke(null, new object[] { Creature })!;
            if (method.Name == predicate)
                result.Should().BeTrue(method.Name);
            else if (method.Name is nameof(EquipmentPredicates.HasSingleWeapon) or nameof(EquipmentPredicates.HasDualWield))
                continue; // these depend on the weapon class, checked on their own below
            else if (predicate == nameof(EquipmentPredicates.HasPistol) && method.Name == nameof(EquipmentPredicates.HasTwinPistol))
                continue;
            else
                result.Should().BeFalse($"{type} is not what {method.Name} looks for");
        }
    }

    [Test]
    public void ThePistolMayBeHeldInEitherHand()
    {
        Left(BaseItem.Pistol);
        EquipmentPredicates.HasPistol(Creature).Should().BeTrue();

        _slots.Clear();
        Right(BaseItem.Pistol);
        EquipmentPredicates.HasPistol(Creature).Should().BeTrue();
    }

    [Test]
    public void AShieldCountsOnlyInTheOffHand()
    {
        Right(BaseItem.LargeShield);
        EquipmentPredicates.HasOffHandShield(Creature).Should().BeFalse();

        _slots.Clear();
        Left(BaseItem.LargeShield);
        EquipmentPredicates.HasOffHandShield(Creature).Should().BeTrue();
    }

    [Test]
    public void KatarsInBothHandsAreADualWieldKatarGrip()
    {
        Right(BaseItem.Katar);
        EquipmentPredicates.HasDualWieldKatar(Creature).Should().BeFalse("only one hand holds one");

        Left(BaseItem.Katar);
        EquipmentPredicates.HasDualWieldKatar(Creature).Should().BeTrue();
        EquipmentPredicates.HasDualWield(Creature).Should().BeTrue();
    }

    [Test]
    public void ASingleWeaponIsAOneHandedWeaponWithAnEmptyOffHand()
    {
        Right(BaseItem.Dagger);
        EquipmentPredicates.HasSingleWeapon(Creature).Should().BeTrue();

        Left(BaseItem.SmallShield);
        EquipmentPredicates.HasSingleWeapon(Creature).Should().BeFalse("the off hand is taken");
    }

    [Test]
    public void ASpearIsNotASingleWeapon()
    {
        Right(BaseItem.Halberd);

        EquipmentPredicates.HasSingleWeapon(Creature).Should().BeFalse();
    }

    [Test]
    public void ADoubleWeaponIsItsOwnOffHandAttackWeapon()
    {
        Right(BaseItem.Saberstaff);

        EquipmentPredicates.GetOffhandAttackWeapon(Creature).Should().Be(RightItem);
        EquipmentPredicates.HasDualWield(Creature).Should().BeTrue("both ends of the staff strike");
    }

    [Test]
    public void ASeparateLeftHandItemIsTheOffHandWeaponButTheCompanionPistolCarrierIsNot()
    {
        Right(BaseItem.Dagger);
        Left(BaseItem.Dagger);
        EquipmentPredicates.GetOffhandAttackWeapon(Creature).Should().Be(LeftItem);
        EquipmentPredicates.HasDualWield(Creature).Should().BeTrue();

        _types[LeftItem] = BaseItem.CompanionPistol;
        EquipmentPredicates.GetOffhandAttackWeapon(Creature).Should().Be(Invalid);
    }

    [Test]
    public void AMainHandWithNoOffHandHasNoOffHandAttackWeapon()
    {
        Right(BaseItem.Dagger);

        EquipmentPredicates.GetOffhandAttackWeapon(Creature).Should().Be(Invalid);
        EquipmentPredicates.HasDualWield(Creature).Should().BeFalse();
    }

    [Test]
    public void ThePowerGloveGripNeedsTheGloveAndItsCompanion()
    {
        EquipmentPredicates.HasUnarmedGrip(BaseItem.PowerGlove, BaseItem.CompanionPowerGlove).Should().BeTrue();
        EquipmentPredicates.HasUnarmedGrip(BaseItem.PowerGlove, null).Should().BeFalse();
        EquipmentPredicates.HasUnarmedGrip(null, BaseItem.CompanionPowerGlove).Should().BeFalse();
        EquipmentPredicates.HasUnarmedGrip(BaseItem.PowerGlove, BaseItem.PowerGlove).Should().BeFalse();
    }
}
