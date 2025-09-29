using NUnit.Framework;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class DroidPerkTests
    {
        [Test]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var perk = new DroidPerk();

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.Invalid));
            Assert.That(perk.Level, Is.EqualTo(0));
        }

        [Test]
        public void ParameterizedConstructor_ShouldSetValues()
        {
            // Act
            var perk = new DroidPerk(PerkType.WeaponFocusVibroblades, 5);

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.WeaponFocusVibroblades));
            Assert.That(perk.Level, Is.EqualTo(5));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var perk = new DroidPerk();

            // Act
            perk.Perk = PerkType.SuperiorWeaponFocus;
            perk.Level = 3;

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.SuperiorWeaponFocus));
            Assert.That(perk.Level, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithDifferentPerkTypes_ShouldWork()
        {
            // Act & Assert
            var perk1 = new DroidPerk(PerkType.ImprovedCriticalVibroblades, 2);
            var perk2 = new DroidPerk(PerkType.PowerAttack, 4);
            var perk3 = new DroidPerk(PerkType.Cleave, 1);

            Assert.That(perk1.Perk, Is.EqualTo(PerkType.ImprovedCriticalVibroblades));
            Assert.That(perk1.Level, Is.EqualTo(2));
            Assert.That(perk2.Perk, Is.EqualTo(PerkType.PowerAttack));
            Assert.That(perk2.Level, Is.EqualTo(4));
            Assert.That(perk3.Perk, Is.EqualTo(PerkType.Cleave));
            Assert.That(perk3.Level, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_WithZeroLevel_ShouldWork()
        {
            // Act
            var perk = new DroidPerk(PerkType.WeaponFocusVibroblades, 0);

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.WeaponFocusVibroblades));
            Assert.That(perk.Level, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithHighLevel_ShouldWork()
        {
            // Act
            var perk = new DroidPerk(PerkType.WeaponFocusVibroblades, 10);

            // Assert
            Assert.That(perk.Perk, Is.EqualTo(PerkType.WeaponFocusVibroblades));
            Assert.That(perk.Level, Is.EqualTo(10));
        }
    }
}
