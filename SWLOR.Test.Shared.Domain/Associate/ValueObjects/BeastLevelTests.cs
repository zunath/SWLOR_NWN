using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class BeastLevelTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var beastLevel = new BeastLevel();

            // Assert
            Assert.That(beastLevel.HP, Is.EqualTo(0));
            Assert.That(beastLevel.STM, Is.EqualTo(0));
            Assert.That(beastLevel.FP, Is.EqualTo(0));
            Assert.That(beastLevel.DMG, Is.EqualTo(0));
            Assert.That(beastLevel.MaxAttackBonus, Is.EqualTo(0));
            Assert.That(beastLevel.MaxAccuracyBonus, Is.EqualTo(0));
            Assert.That(beastLevel.MaxEvasionBonus, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_ShouldInitializeStatsDictionary()
        {
            // Act
            var beastLevel = new BeastLevel();

            // Assert
            Assert.That(beastLevel.Stats, Is.Not.Null);
            Assert.That(beastLevel.Stats.Count, Is.EqualTo(6));
            Assert.That(beastLevel.Stats[AbilityType.Might], Is.EqualTo(0));
            Assert.That(beastLevel.Stats[AbilityType.Perception], Is.EqualTo(0));
            Assert.That(beastLevel.Stats[AbilityType.Vitality], Is.EqualTo(0));
            Assert.That(beastLevel.Stats[AbilityType.Willpower], Is.EqualTo(0));
            Assert.That(beastLevel.Stats[AbilityType.Agility], Is.EqualTo(0));
            Assert.That(beastLevel.Stats[AbilityType.Social], Is.EqualTo(0));
        }

        [Test]
        public void Constructor_ShouldInitializeMaxDefenseBonusesDictionary()
        {
            // Act
            var beastLevel = new BeastLevel();

            // Assert
            Assert.That(beastLevel.MaxDefenseBonuses, Is.Not.Null);
            Assert.That(beastLevel.MaxDefenseBonuses.Count, Is.EqualTo(6));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Physical], Is.EqualTo(0));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Force], Is.EqualTo(0));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Fire], Is.EqualTo(0));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Poison], Is.EqualTo(0));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Electrical], Is.EqualTo(0));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Ice], Is.EqualTo(0));
        }

        [Test]
        public void Constructor_ShouldInitializeMaxSavingThrowBonusesDictionary()
        {
            // Act
            var beastLevel = new BeastLevel();

            // Assert
            Assert.That(beastLevel.MaxSavingThrowBonuses, Is.Not.Null);
            Assert.That(beastLevel.MaxSavingThrowBonuses.Count, Is.EqualTo(3));
            Assert.That(beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Fortitude], Is.EqualTo(0));
            Assert.That(beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Will], Is.EqualTo(0));
            Assert.That(beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Reflex], Is.EqualTo(0));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var beastLevel = new BeastLevel();

            // Act
            beastLevel.HP = 100;
            beastLevel.STM = 50;
            beastLevel.FP = 25;
            beastLevel.DMG = 10;
            beastLevel.MaxAttackBonus = 5;
            beastLevel.MaxAccuracyBonus = 3;
            beastLevel.MaxEvasionBonus = 2;

            // Assert
            Assert.That(beastLevel.HP, Is.EqualTo(100));
            Assert.That(beastLevel.STM, Is.EqualTo(50));
            Assert.That(beastLevel.FP, Is.EqualTo(25));
            Assert.That(beastLevel.DMG, Is.EqualTo(10));
            Assert.That(beastLevel.MaxAttackBonus, Is.EqualTo(5));
            Assert.That(beastLevel.MaxAccuracyBonus, Is.EqualTo(3));
            Assert.That(beastLevel.MaxEvasionBonus, Is.EqualTo(2));
        }

        [Test]
        public void Stats_ShouldBeMutable()
        {
            // Arrange
            var beastLevel = new BeastLevel();

            // Act
            beastLevel.Stats[AbilityType.Might] = 15;
            beastLevel.Stats[AbilityType.Perception] = 12;

            // Assert
            Assert.That(beastLevel.Stats[AbilityType.Might], Is.EqualTo(15));
            Assert.That(beastLevel.Stats[AbilityType.Perception], Is.EqualTo(12));
        }

        [Test]
        public void MaxDefenseBonuses_ShouldBeMutable()
        {
            // Arrange
            var beastLevel = new BeastLevel();

            // Act
            beastLevel.MaxDefenseBonuses[CombatDamageType.Physical] = 5;
            beastLevel.MaxDefenseBonuses[CombatDamageType.Force] = 3;

            // Assert
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Physical], Is.EqualTo(5));
            Assert.That(beastLevel.MaxDefenseBonuses[CombatDamageType.Force], Is.EqualTo(3));
        }

        [Test]
        public void MaxSavingThrowBonuses_ShouldBeMutable()
        {
            // Arrange
            var beastLevel = new BeastLevel();

            // Act
            beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Fortitude] = 4;
            beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Will] = 2;

            // Assert
            Assert.That(beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Fortitude], Is.EqualTo(4));
            Assert.That(beastLevel.MaxSavingThrowBonuses[SavingThrowCategoryType.Will], Is.EqualTo(2));
        }
    }
}
