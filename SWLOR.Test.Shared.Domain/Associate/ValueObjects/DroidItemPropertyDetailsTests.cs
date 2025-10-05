using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class DroidItemPropertyDetailsTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var details = new DroidItemPropertyDetails();

            // Assert
            Assert.That(details.CustomName, Is.EqualTo(string.Empty));
            Assert.That(details.Tier, Is.EqualTo(1));
            Assert.That(details.Level, Is.EqualTo(0));
            Assert.That(details.HP, Is.EqualTo(0));
            Assert.That(details.STM, Is.EqualTo(0));
            Assert.That(details.AISlots, Is.EqualTo(0));
            Assert.That(details.AGI, Is.EqualTo(0));
            Assert.That(details.MGT, Is.EqualTo(0));
            Assert.That(details.PER, Is.EqualTo(0));
            Assert.That(details.SOC, Is.EqualTo(0));
            Assert.That(details.VIT, Is.EqualTo(0));
            Assert.That(details.WIL, Is.EqualTo(0));
            Assert.That(details.PersonalityType, Is.EqualTo(DroidPersonalityType.Bland));
            Assert.That(details.Skills, Is.Not.Null);
            Assert.That(details.Skills, Is.Empty);
            Assert.That(details.Perks, Is.Not.Null);
            Assert.That(details.Perks, Is.Empty);
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var details = new DroidItemPropertyDetails();

            // Act
            details.CustomName = "Test Droid";
            details.Tier = 5;
            details.Level = 10;
            details.HP = 100;
            details.STM = 50;
            details.AISlots = 3;
            details.AGI = 15;
            details.MGT = 12;
            details.PER = 18;
            details.SOC = 8;
            details.VIT = 20;
            details.WIL = 14;
            details.PersonalityType = DroidPersonalityType.Sarcastic;
            details.Skills.Add(SkillType.OneHanded, 5);
            details.Perks.Add(PerkType.WeaponFocusVibroblades, 3);

            // Assert
            Assert.That(details.CustomName, Is.EqualTo("Test Droid"));
            Assert.That(details.Tier, Is.EqualTo(5));
            Assert.That(details.Level, Is.EqualTo(10));
            Assert.That(details.HP, Is.EqualTo(100));
            Assert.That(details.STM, Is.EqualTo(50));
            Assert.That(details.AISlots, Is.EqualTo(3));
            Assert.That(details.AGI, Is.EqualTo(15));
            Assert.That(details.MGT, Is.EqualTo(12));
            Assert.That(details.PER, Is.EqualTo(18));
            Assert.That(details.SOC, Is.EqualTo(8));
            Assert.That(details.VIT, Is.EqualTo(20));
            Assert.That(details.WIL, Is.EqualTo(14));
            Assert.That(details.PersonalityType, Is.EqualTo(DroidPersonalityType.Sarcastic));
            Assert.That(details.Skills.Count, Is.EqualTo(1));
            Assert.That(details.Perks.Count, Is.EqualTo(1));
        }

        [Test]
        public void Skills_ShouldBeMutable()
        {
            // Arrange
            var details = new DroidItemPropertyDetails();

            // Act
            details.Skills.Add(SkillType.OneHanded, 5);
            details.Skills.Add(SkillType.TwoHanded, 3);
            details.Skills.Add(SkillType.MartialArts, 7);

            // Assert
            Assert.That(details.Skills.Count, Is.EqualTo(3));
            Assert.That(details.Skills[SkillType.OneHanded], Is.EqualTo(5));
            Assert.That(details.Skills[SkillType.TwoHanded], Is.EqualTo(3));
            Assert.That(details.Skills[SkillType.MartialArts], Is.EqualTo(7));
        }

        [Test]
        public void Perks_ShouldBeMutable()
        {
            // Arrange
            var details = new DroidItemPropertyDetails();

            // Act
            details.Perks.Add(PerkType.WeaponFocusVibroblades, 3);
            details.Perks.Add(PerkType.SuperiorWeaponFocus, 2);
            details.Perks.Add(PerkType.ImprovedCriticalVibroblades, 1);

            // Assert
            Assert.That(details.Perks.Count, Is.EqualTo(3));
            Assert.That(details.Perks[PerkType.WeaponFocusVibroblades], Is.EqualTo(3));
            Assert.That(details.Perks[PerkType.SuperiorWeaponFocus], Is.EqualTo(2));
            Assert.That(details.Perks[PerkType.ImprovedCriticalVibroblades], Is.EqualTo(1));
        }
    }
}
