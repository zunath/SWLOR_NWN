using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Test.Shared.Domain.Entities
{
    [TestFixture]
    public class BeastTests
    {
        [Test]
        public void Beast_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var beast = new Beast();

            // Assert
            Assert.That(beast.Id, Is.Not.Null);
            Assert.That(beast.Name, Is.Null);
            Assert.That(beast.OwnerPlayerId, Is.Null);
            Assert.That(beast.Level, Is.EqualTo(1));
            Assert.That(beast.XP, Is.EqualTo(0));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(0));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(0));
            Assert.That(beast.IsDead, Is.False);
            Assert.That(beast.PortraitId, Is.EqualTo(-1));
            Assert.That(beast.SoundSetId, Is.EqualTo(-1));
            Assert.That(beast.Type, Is.EqualTo(BeastType.Invalid));
            Assert.That(beast.FavoriteFood, Is.EqualTo(BeastFoodType.Invalid));
            Assert.That(beast.HatedFood, Is.EqualTo(BeastFoodType.Invalid));
            Assert.That(beast.Perks, Is.Not.Null);
            Assert.That(beast.AttackPurity, Is.EqualTo(0));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(0));
            Assert.That(beast.EvasionPurity, Is.EqualTo(0));
            Assert.That(beast.LearningPurity, Is.EqualTo(0));
            Assert.That(beast.DefensePurities, Is.Not.Null);
            Assert.That(beast.SavingThrowPurities, Is.Not.Null);
        }

        [Test]
        public void Beast_WithName_ShouldStoreNameCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Name = "Test Beast";

            // Assert
            Assert.That(beast.Name, Is.EqualTo("Test Beast"));
        }

        [Test]
        public void Beast_WithOwnerPlayerId_ShouldStoreOwnerPlayerIdCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.OwnerPlayerId = "player-123";

            // Assert
            Assert.That(beast.OwnerPlayerId, Is.EqualTo("player-123"));
        }

        [Test]
        public void Beast_WithLevel_ShouldStoreLevelCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = 5;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(5));
        }

        [Test]
        public void Beast_WithXP_ShouldStoreXPCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.XP = 1000;

            // Assert
            Assert.That(beast.XP, Is.EqualTo(1000));
        }

        [Test]
        public void Beast_WithXPPenaltyPercent_ShouldStoreXPPenaltyPercentCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.XPPenaltyPercent = 25;

            // Assert
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(25));
        }

        [Test]
        public void Beast_WithUnallocatedSP_ShouldStoreUnallocatedSPCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.UnallocatedSP = 50;

            // Assert
            Assert.That(beast.UnallocatedSP, Is.EqualTo(50));
        }

        [Test]
        public void Beast_WithIsDead_ShouldStoreIsDeadCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.IsDead = true;

            // Assert
            Assert.That(beast.IsDead, Is.True);
        }

        [Test]
        public void Beast_WithPortraitId_ShouldStorePortraitIdCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.PortraitId = 5;

            // Assert
            Assert.That(beast.PortraitId, Is.EqualTo(5));
        }

        [Test]
        public void Beast_WithSoundSetId_ShouldStoreSoundSetIdCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.SoundSetId = 3;

            // Assert
            Assert.That(beast.SoundSetId, Is.EqualTo(3));
        }

        [Test]
        public void Beast_WithType_ShouldStoreTypeCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Type = BeastType.KathHound;

            // Assert
            Assert.That(beast.Type, Is.EqualTo(BeastType.KathHound));
        }

        [Test]
        public void Beast_WithFavoriteFood_ShouldStoreFavoriteFoodCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.FavoriteFood = BeastFoodType.CookedMeat;

            // Assert
            Assert.That(beast.FavoriteFood, Is.EqualTo(BeastFoodType.CookedMeat));
        }

        [Test]
        public void Beast_WithHatedFood_ShouldStoreHatedFoodCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.HatedFood = BeastFoodType.SourFruit;

            // Assert
            Assert.That(beast.HatedFood, Is.EqualTo(BeastFoodType.SourFruit));
        }

        [Test]
        public void Beast_WithPerks_ShouldStorePerksCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Perks[PerkType.LightsaberProficiency] = 3;
            beast.Perks[PerkType.ForceLeap] = 2;
            beast.Perks[PerkType.VibrobladeProficiency] = 1;

            // Assert
            Assert.That(beast.Perks[PerkType.LightsaberProficiency], Is.EqualTo(3));
            Assert.That(beast.Perks[PerkType.ForceLeap], Is.EqualTo(2));
            Assert.That(beast.Perks[PerkType.VibrobladeProficiency], Is.EqualTo(1));
        }

        [Test]
        public void Beast_WithPurities_ShouldStorePuritiesCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.AttackPurity = 10;
            beast.AccuracyPurity = 15;
            beast.EvasionPurity = 12;
            beast.LearningPurity = 8;

            // Assert
            Assert.That(beast.AttackPurity, Is.EqualTo(10));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(15));
            Assert.That(beast.EvasionPurity, Is.EqualTo(12));
            Assert.That(beast.LearningPurity, Is.EqualTo(8));
        }

        [Test]
        public void Beast_WithDefensePurities_ShouldInitializeWithAllDamageTypes()
        {
            // Act
            var beast = new Beast();

            // Assert
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Physical), Is.True);
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Force), Is.True);
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Fire), Is.True);
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Ice), Is.True);
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Poison), Is.True);
            Assert.That(beast.DefensePurities.ContainsKey(CombatDamageType.Electrical), Is.True);

            foreach (var defense in beast.DefensePurities.Values)
            {
                Assert.That(defense, Is.EqualTo(0));
            }
        }

        [Test]
        public void Beast_WithSavingThrowPurities_ShouldInitializeWithAllSavingThrowTypes()
        {
            // Act
            var beast = new Beast();

            // Assert
            Assert.That(beast.SavingThrowPurities.ContainsKey(SavingThrowCategoryType.Fortitude), Is.True);
            Assert.That(beast.SavingThrowPurities.ContainsKey(SavingThrowCategoryType.Will), Is.True);
            Assert.That(beast.SavingThrowPurities.ContainsKey(SavingThrowCategoryType.Reflex), Is.True);

            foreach (var savingThrow in beast.SavingThrowPurities.Values)
            {
                Assert.That(savingThrow, Is.EqualTo(0));
            }
        }

        [Test]
        public void Beast_WithDefensePurities_ShouldStoreDefensePuritiesCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.DefensePurities[CombatDamageType.Physical] = 10;
            beast.DefensePurities[CombatDamageType.Force] = 15;
            beast.DefensePurities[CombatDamageType.Fire] = 5;

            // Assert
            Assert.That(beast.DefensePurities[CombatDamageType.Physical], Is.EqualTo(10));
            Assert.That(beast.DefensePurities[CombatDamageType.Force], Is.EqualTo(15));
            Assert.That(beast.DefensePurities[CombatDamageType.Fire], Is.EqualTo(5));
        }

        [Test]
        public void Beast_WithSavingThrowPurities_ShouldStoreSavingThrowPuritiesCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude] = 8;
            beast.SavingThrowPurities[SavingThrowCategoryType.Will] = 12;
            beast.SavingThrowPurities[SavingThrowCategoryType.Reflex] = 6;

            // Assert
            Assert.That(beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude], Is.EqualTo(8));
            Assert.That(beast.SavingThrowPurities[SavingThrowCategoryType.Will], Is.EqualTo(12));
            Assert.That(beast.SavingThrowPurities[SavingThrowCategoryType.Reflex], Is.EqualTo(6));
        }

        [Test]
        public void Beast_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Name = "Test Beast";
            beast.OwnerPlayerId = "player-123";
            beast.Level = 5;
            beast.XP = 1000;
            beast.XPPenaltyPercent = 25;
            beast.UnallocatedSP = 50;
            beast.IsDead = false;
            beast.PortraitId = 5;
            beast.SoundSetId = 3;
            beast.Type = BeastType.KathHound;
            beast.FavoriteFood = BeastFoodType.CookedMeat;
            beast.HatedFood = BeastFoodType.SourFruit;
            beast.Perks[PerkType.LightsaberProficiency] = 3;
            beast.AttackPurity = 10;
            beast.AccuracyPurity = 15;
            beast.EvasionPurity = 12;
            beast.LearningPurity = 8;
            beast.DefensePurities[CombatDamageType.Physical] = 10;
            beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude] = 8;

            // Assert
            Assert.That(beast.Name, Is.EqualTo("Test Beast"));
            Assert.That(beast.OwnerPlayerId, Is.EqualTo("player-123"));
            Assert.That(beast.Level, Is.EqualTo(5));
            Assert.That(beast.XP, Is.EqualTo(1000));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(25));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(50));
            Assert.That(beast.IsDead, Is.False);
            Assert.That(beast.PortraitId, Is.EqualTo(5));
            Assert.That(beast.SoundSetId, Is.EqualTo(3));
            Assert.That(beast.Type, Is.EqualTo(BeastType.KathHound));
            Assert.That(beast.FavoriteFood, Is.EqualTo(BeastFoodType.CookedMeat));
            Assert.That(beast.HatedFood, Is.EqualTo(BeastFoodType.SourFruit));
            Assert.That(beast.Perks[PerkType.LightsaberProficiency], Is.EqualTo(3));
            Assert.That(beast.AttackPurity, Is.EqualTo(10));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(15));
            Assert.That(beast.EvasionPurity, Is.EqualTo(12));
            Assert.That(beast.LearningPurity, Is.EqualTo(8));
            Assert.That(beast.DefensePurities[CombatDamageType.Physical], Is.EqualTo(10));
            Assert.That(beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude], Is.EqualTo(8));
        }

        [Test]
        public void Beast_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = 0;
            beast.XP = 0;
            beast.XPPenaltyPercent = 0;
            beast.UnallocatedSP = 0;
            beast.AttackPurity = 0;
            beast.AccuracyPurity = 0;
            beast.EvasionPurity = 0;
            beast.LearningPurity = 0;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(0));
            Assert.That(beast.XP, Is.EqualTo(0));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(0));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(0));
            Assert.That(beast.AttackPurity, Is.EqualTo(0));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(0));
            Assert.That(beast.EvasionPurity, Is.EqualTo(0));
            Assert.That(beast.LearningPurity, Is.EqualTo(0));
        }

        [Test]
        public void Beast_WithNegativeValues_ShouldStoreNegativeValues()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = -1;
            beast.XP = -100;
            beast.XPPenaltyPercent = -25;
            beast.UnallocatedSP = -50;
            beast.AttackPurity = -5;
            beast.AccuracyPurity = -10;
            beast.EvasionPurity = -8;
            beast.LearningPurity = -3;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(-1));
            Assert.That(beast.XP, Is.EqualTo(-100));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(-25));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(-50));
            Assert.That(beast.AttackPurity, Is.EqualTo(-5));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(-10));
            Assert.That(beast.EvasionPurity, Is.EqualTo(-8));
            Assert.That(beast.LearningPurity, Is.EqualTo(-3));
        }

        [Test]
        public void Beast_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = 100;
            beast.XP = 1000000;
            beast.XPPenaltyPercent = 100;
            beast.UnallocatedSP = 1000;
            beast.AttackPurity = 100;
            beast.AccuracyPurity = 100;
            beast.EvasionPurity = 100;
            beast.LearningPurity = 100;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(100));
            Assert.That(beast.XP, Is.EqualTo(1000000));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(100));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(1000));
            Assert.That(beast.AttackPurity, Is.EqualTo(100));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(100));
            Assert.That(beast.EvasionPurity, Is.EqualTo(100));
            Assert.That(beast.LearningPurity, Is.EqualTo(100));
        }

        [Test]
        public void Beast_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = int.MaxValue;
            beast.XP = int.MaxValue;
            beast.XPPenaltyPercent = int.MaxValue;
            beast.UnallocatedSP = int.MaxValue;
            beast.AttackPurity = int.MaxValue;
            beast.AccuracyPurity = int.MaxValue;
            beast.EvasionPurity = int.MaxValue;
            beast.LearningPurity = int.MaxValue;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(int.MaxValue));
            Assert.That(beast.XP, Is.EqualTo(int.MaxValue));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(int.MaxValue));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(int.MaxValue));
            Assert.That(beast.AttackPurity, Is.EqualTo(int.MaxValue));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(int.MaxValue));
            Assert.That(beast.EvasionPurity, Is.EqualTo(int.MaxValue));
            Assert.That(beast.LearningPurity, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Beast_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Level = int.MinValue;
            beast.XP = int.MinValue;
            beast.XPPenaltyPercent = int.MinValue;
            beast.UnallocatedSP = int.MinValue;
            beast.AttackPurity = int.MinValue;
            beast.AccuracyPurity = int.MinValue;
            beast.EvasionPurity = int.MinValue;
            beast.LearningPurity = int.MinValue;

            // Assert
            Assert.That(beast.Level, Is.EqualTo(int.MinValue));
            Assert.That(beast.XP, Is.EqualTo(int.MinValue));
            Assert.That(beast.XPPenaltyPercent, Is.EqualTo(int.MinValue));
            Assert.That(beast.UnallocatedSP, Is.EqualTo(int.MinValue));
            Assert.That(beast.AttackPurity, Is.EqualTo(int.MinValue));
            Assert.That(beast.AccuracyPurity, Is.EqualTo(int.MinValue));
            Assert.That(beast.EvasionPurity, Is.EqualTo(int.MinValue));
            Assert.That(beast.LearningPurity, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void Beast_WithPerkIncrement_ShouldIncrementPerk()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Perks[PerkType.LightsaberProficiency] = 1;
            beast.Perks[PerkType.LightsaberProficiency]++;

            // Assert
            Assert.That(beast.Perks[PerkType.LightsaberProficiency], Is.EqualTo(2));
        }

        [Test]
        public void Beast_WithPerkDecrement_ShouldDecrementPerk()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.Perks[PerkType.LightsaberProficiency] = 5;
            beast.Perks[PerkType.LightsaberProficiency]--;

            // Assert
            Assert.That(beast.Perks[PerkType.LightsaberProficiency], Is.EqualTo(4));
        }

        [Test]
        public void Beast_WithPerkRemoval_ShouldRemovePerk()
        {
            // Arrange
            var beast = new Beast();
            beast.Perks[PerkType.LightsaberProficiency] = 3;

            // Act
            beast.Perks.Remove(PerkType.LightsaberProficiency);

            // Assert
            Assert.That(beast.Perks.ContainsKey(PerkType.LightsaberProficiency), Is.False);
        }

        [Test]
        public void Beast_WithDefensePurityIncrement_ShouldIncrementDefensePurity()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.DefensePurities[CombatDamageType.Physical] = 5;
            beast.DefensePurities[CombatDamageType.Physical] += 3;

            // Assert
            Assert.That(beast.DefensePurities[CombatDamageType.Physical], Is.EqualTo(8));
        }

        [Test]
        public void Beast_WithSavingThrowPurityIncrement_ShouldIncrementSavingThrowPurity()
        {
            // Arrange
            var beast = new Beast();

            // Act
            beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude] = 5;
            beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude] += 2;

            // Assert
            Assert.That(beast.SavingThrowPurities[SavingThrowCategoryType.Fortitude], Is.EqualTo(7));
        }

        [Test]
        public void Beast_WithSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var beast = new Beast();
            beast.Name = "Test Beast";
            beast.Level = 5;
            beast.XP = 1000;
            beast.Type = BeastType.KathHound;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(beast);
            var deserializedBeast = System.Text.Json.JsonSerializer.Deserialize<Beast>(json);

            // Assert
            Assert.That(deserializedBeast, Is.Not.Null);
            Assert.That(deserializedBeast.Name, Is.EqualTo(beast.Name));
            Assert.That(deserializedBeast.Level, Is.EqualTo(beast.Level));
            Assert.That(deserializedBeast.XP, Is.EqualTo(beast.XP));
            Assert.That(deserializedBeast.Type, Is.EqualTo(beast.Type));
        }

        [Test]
        public void Beast_WithEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var beast1 = new Beast();
            var beast2 = new Beast();
            beast1.Level = 5;
            beast2.Level = 5;

            // Act & Assert
            Assert.That(beast1.Level, Is.EqualTo(beast2.Level));
        }

        [Test]
        public void Beast_WithInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var beast1 = new Beast();
            var beast2 = new Beast();
            beast1.Level = 5;
            beast2.Level = 10;

            // Act & Assert
            Assert.That(beast1.Level, Is.Not.EqualTo(beast2.Level));
        }

        [Test]
        public void Beast_WithToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var beast = new Beast();
            beast.Name = "Test Beast";

            // Act
            var result = beast.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void Beast_WithGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var beast = new Beast();

            // Act
            var type = beast.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(Beast)));
        }

        [Test]
        public void Beast_WithHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var beast = new Beast();
            beast.Name = "Test Beast";

            // Act
            var hashCode = beast.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }
    }
}
