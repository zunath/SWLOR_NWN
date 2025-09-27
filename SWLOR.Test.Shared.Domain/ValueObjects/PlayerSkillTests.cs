using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Test.Shared.Core.TestHelpers;

namespace SWLOR.Test.Shared.Domain.ValueObjects
{
    [TestFixture]
    public class PlayerSkillTests
    {
        [Test]
        public void PlayerSkill_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var playerSkill = new PlayerSkill();

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(0));
            Assert.That(playerSkill.XP, Is.EqualTo(0));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithRank_ShouldStoreRankCorrectly()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = 5;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(5));
        }

        [Test]
        public void PlayerSkill_WithXP_ShouldStoreXPCorrectly()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.XP = 250;

            // Assert
            Assert.That(playerSkill.XP, Is.EqualTo(250));
        }

        [Test]
        public void PlayerSkill_WithIsLocked_ShouldStoreIsLockedCorrectly()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.IsLocked = true;

            // Assert
            Assert.That(playerSkill.IsLocked, Is.True);
        }

        [Test]
        public void PlayerSkill_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = 7;
            playerSkill.XP = 350;
            playerSkill.IsLocked = false;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(7));
            Assert.That(playerSkill.XP, Is.EqualTo(350));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithNegativeRank_ShouldStoreNegativeRank()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = -1;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(-1));
        }

        [Test]
        public void PlayerSkill_WithNegativeXP_ShouldStoreNegativeXP()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.XP = -100;

            // Assert
            Assert.That(playerSkill.XP, Is.EqualTo(-100));
        }

        [Test]
        public void PlayerSkill_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = 100;
            playerSkill.XP = 999999;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(100));
            Assert.That(playerSkill.XP, Is.EqualTo(999999));
        }

        [Test]
        public void PlayerSkill_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = 0;
            playerSkill.XP = 0;
            playerSkill.IsLocked = false;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(0));
            Assert.That(playerSkill.XP, Is.EqualTo(0));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = int.MaxValue;
            playerSkill.XP = int.MaxValue;
            playerSkill.IsLocked = true;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(int.MaxValue));
            Assert.That(playerSkill.XP, Is.EqualTo(int.MaxValue));
            Assert.That(playerSkill.IsLocked, Is.True);
        }

        [Test]
        public void PlayerSkill_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            playerSkill.Rank = int.MinValue;
            playerSkill.XP = int.MinValue;
            playerSkill.IsLocked = false;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(int.MinValue));
            Assert.That(playerSkill.XP, Is.EqualTo(int.MinValue));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithSkillType_ShouldWorkWithSkillType()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Skills[SkillType.OneHanded] = new PlayerSkill 
            { 
                Rank = 5, 
                XP = 250, 
                IsLocked = false 
            };

            // Assert
            Assert.That(player.Skills[SkillType.OneHanded].Rank, Is.EqualTo(5));
            Assert.That(player.Skills[SkillType.OneHanded].XP, Is.EqualTo(250));
            Assert.That(player.Skills[SkillType.OneHanded].IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithMultipleSkills_ShouldWorkWithMultipleSkills()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Skills[SkillType.OneHanded] = new PlayerSkill 
            { 
                Rank = 5, 
                XP = 250, 
                IsLocked = false 
            };
            
            player.Skills[SkillType.TwoHanded] = new PlayerSkill 
            { 
                Rank = 3, 
                XP = 150, 
                IsLocked = true 
            };
            
            player.Skills[SkillType.Force] = new PlayerSkill 
            { 
                Rank = 7, 
                XP = 350, 
                IsLocked = false 
            };

            // Assert
            Assert.That(player.Skills[SkillType.OneHanded].Rank, Is.EqualTo(5));
            Assert.That(player.Skills[SkillType.OneHanded].XP, Is.EqualTo(250));
            Assert.That(player.Skills[SkillType.OneHanded].IsLocked, Is.False);

            Assert.That(player.Skills[SkillType.TwoHanded].Rank, Is.EqualTo(3));
            Assert.That(player.Skills[SkillType.TwoHanded].XP, Is.EqualTo(150));
            Assert.That(player.Skills[SkillType.TwoHanded].IsLocked, Is.True);

            Assert.That(player.Skills[SkillType.Force].Rank, Is.EqualTo(7));
            Assert.That(player.Skills[SkillType.Force].XP, Is.EqualTo(350));
            Assert.That(player.Skills[SkillType.Force].IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithSkillProgression_ShouldSimulateSkillProgression()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var playerSkill = new PlayerSkill { Rank = 1, XP = 0, IsLocked = false };

            // Act - Simulate gaining XP
            playerSkill.XP += 100;
            
            // Simulate rank up
            if (playerSkill.XP >= 200)
            {
                playerSkill.Rank++;
                playerSkill.XP -= 200;
            }

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(1));
            Assert.That(playerSkill.XP, Is.EqualTo(100));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithSkillLocking_ShouldSimulateSkillLocking()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act - Lock the skill
            playerSkill.IsLocked = true;

            // Assert
            Assert.That(playerSkill.IsLocked, Is.True);
            Assert.That(playerSkill.Rank, Is.EqualTo(5));
            Assert.That(playerSkill.XP, Is.EqualTo(250));
        }

        [Test]
        public void PlayerSkill_WithSkillUnlocking_ShouldSimulateSkillUnlocking()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = true };

            // Act - Unlock the skill
            playerSkill.IsLocked = false;

            // Assert
            Assert.That(playerSkill.IsLocked, Is.False);
            Assert.That(playerSkill.Rank, Is.EqualTo(5));
            Assert.That(playerSkill.XP, Is.EqualTo(250));
        }

        [Test]
        public void PlayerSkill_WithSkillReset_ShouldSimulateSkillReset()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act - Reset the skill
            playerSkill.Rank = 0;
            playerSkill.XP = 0;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(0));
            Assert.That(playerSkill.XP, Is.EqualTo(0));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithSkillModification_ShouldSimulateSkillModification()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 3, XP = 150, IsLocked = false };

            // Act - Modify the skill
            playerSkill.Rank += 2;
            playerSkill.XP += 100;

            // Assert
            Assert.That(playerSkill.Rank, Is.EqualTo(5));
            Assert.That(playerSkill.XP, Is.EqualTo(250));
            Assert.That(playerSkill.IsLocked, Is.False);
        }

        [Test]
        public void PlayerSkill_WithSkillComparison_ShouldCompareSkillsCorrectly()
        {
            // Arrange
            var playerSkill1 = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            var playerSkill2 = new PlayerSkill { Rank = 3, XP = 150, IsLocked = false };
            var playerSkill3 = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act & Assert
            Assert.That(playerSkill1.Rank, Is.GreaterThan(playerSkill2.Rank));
            Assert.That(playerSkill1.XP, Is.GreaterThan(playerSkill2.XP));
            Assert.That(playerSkill1.Rank, Is.EqualTo(playerSkill3.Rank));
            Assert.That(playerSkill1.XP, Is.EqualTo(playerSkill3.XP));
        }

        [Test]
        public void PlayerSkill_WithSkillValidation_ShouldValidateSkillsCorrectly()
        {
            // Arrange
            var validSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            var invalidSkill = new PlayerSkill { Rank = -1, XP = -100, IsLocked = false };

            // Act & Assert
            Assert.That(validSkill.Rank, Is.GreaterThanOrEqualTo(0));
            Assert.That(validSkill.XP, Is.GreaterThanOrEqualTo(0));
            Assert.That(invalidSkill.Rank, Is.LessThan(0));
            Assert.That(invalidSkill.XP, Is.LessThan(0));
        }

        [Test]
        public void PlayerSkill_WithSkillEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var playerSkill1 = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            var playerSkill2 = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            var playerSkill3 = new PlayerSkill { Rank = 5, XP = 250, IsLocked = true };

            // Act & Assert
            Assert.That(playerSkill1.Rank, Is.EqualTo(playerSkill2.Rank));
            Assert.That(playerSkill1.XP, Is.EqualTo(playerSkill2.XP));
            Assert.That(playerSkill1.IsLocked, Is.EqualTo(playerSkill2.IsLocked));
            Assert.That(playerSkill1.IsLocked, Is.Not.EqualTo(playerSkill3.IsLocked));
        }

        [Test]
        public void PlayerSkill_WithSkillToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act
            var result = playerSkill.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void PlayerSkill_WithSkillHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act
            var hashCode = playerSkill.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }


        [Test]
        public void PlayerSkill_WithSkillGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var playerSkill = new PlayerSkill();

            // Act
            var type = playerSkill.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(PlayerSkill)));
        }


        [Test]
        public void PlayerSkill_WithSkillSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var playerSkill = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(playerSkill);
            var deserializedSkill = System.Text.Json.JsonSerializer.Deserialize<PlayerSkill>(json);

            // Assert
            Assert.That(deserializedSkill, Is.Not.Null);
            Assert.That(deserializedSkill.Rank, Is.EqualTo(playerSkill.Rank));
            Assert.That(deserializedSkill.XP, Is.EqualTo(playerSkill.XP));
            Assert.That(deserializedSkill.IsLocked, Is.EqualTo(playerSkill.IsLocked));
        }
    }
}
