using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Test.Shared.Core.TestHelpers;

namespace SWLOR.Test.Shared.Domain.Entities
{
    [TestFixture]
    public class AccountTests
    {
        [Test]
        public void Account_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var account = new Account();

            // Assert
            Assert.That(account.Id, Is.Not.Null);
            Assert.That(account.TimesLoggedIn, Is.EqualTo(0));
            Assert.That(account.HasCompletedTutorial, Is.False);
            Assert.That(account.Achievements, Is.Not.Null);
            Assert.That(account.AchievementProgress, Is.Not.Null);
        }

        [Test]
        public void Account_ConstructorWithCdKey_ShouldSetId()
        {
            // Arrange
            const string cdKey = "test-cd-key-123";

            // Act
            var account = new Account(cdKey);

            // Assert
            Assert.That(account.Id, Is.EqualTo(cdKey));
            Assert.That(account.TimesLoggedIn, Is.EqualTo(0));
            Assert.That(account.HasCompletedTutorial, Is.False);
            Assert.That(account.Achievements, Is.Not.Null);
            Assert.That(account.AchievementProgress, Is.Not.Null);
        }

        [Test]
        public void Account_WithTimesLoggedIn_ShouldStoreTimesLoggedInCorrectly()
        {
            // Arrange
            var account = new Account();

            // Act
            account.TimesLoggedIn = 5;

            // Assert
            Assert.That(account.TimesLoggedIn, Is.EqualTo(5));
        }

        [Test]
        public void Account_WithTutorialCompletion_ShouldStoreTutorialCompletionCorrectly()
        {
            // Arrange
            var account = new Account();

            // Act
            account.HasCompletedTutorial = true;

            // Assert
            Assert.That(account.HasCompletedTutorial, Is.True);
        }

        [Test]
        public void Account_WithAchievements_ShouldStoreAchievementsCorrectly()
        {
            // Arrange
            var account = new Account();
            var achievementDate = DateTime.Now.AddDays(-1);

            // Act
            account.Achievements[AchievementType.KillEnemies1] = achievementDate;
            account.Achievements[AchievementType.CraftItems1] = DateTime.Now;

            // Assert
            Assert.That(account.Achievements[AchievementType.KillEnemies1], Is.EqualTo(achievementDate));
            Assert.That(account.Achievements[AchievementType.CraftItems1], Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Account_WithAchievementProgress_ShouldStoreAchievementProgressCorrectly()
        {
            // Arrange
            var account = new Account();

            // Act
            account.AchievementProgress.EnemiesKilled = 100;
            account.AchievementProgress.PerksLearned = 25;
            account.AchievementProgress.SkillsLearned = 15;
            account.AchievementProgress.QuestsCompleted = 10;
            account.AchievementProgress.ItemsCrafted = 50;

            // Assert
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(100));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(25));
            Assert.That(account.AchievementProgress.SkillsLearned, Is.EqualTo(15));
            Assert.That(account.AchievementProgress.QuestsCompleted, Is.EqualTo(10));
            Assert.That(account.AchievementProgress.ItemsCrafted, Is.EqualTo(50));
        }

        [Test]
        public void Account_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var account = new Account("test-cd-key-456");
            var achievementDate = DateTime.Now.AddDays(-2);

            // Act
            account.TimesLoggedIn = 10;
            account.HasCompletedTutorial = true;
            account.Achievements[AchievementType.KillEnemies1] = achievementDate;
            account.AchievementProgress.EnemiesKilled = 200;
            account.AchievementProgress.PerksLearned = 50;

            // Assert
            Assert.That(account.Id, Is.EqualTo("test-cd-key-456"));
            Assert.That(account.TimesLoggedIn, Is.EqualTo(10));
            Assert.That(account.HasCompletedTutorial, Is.True);
            Assert.That(account.Achievements[AchievementType.KillEnemies1], Is.EqualTo(achievementDate));
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(200));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(50));
        }

        [Test]
        public void Account_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var account = new Account();

            // Act
            account.TimesLoggedIn = 0;
            account.HasCompletedTutorial = false;
            account.AchievementProgress.EnemiesKilled = 0;
            account.AchievementProgress.PerksLearned = 0;
            account.AchievementProgress.SkillsLearned = 0;
            account.AchievementProgress.QuestsCompleted = 0;
            account.AchievementProgress.ItemsCrafted = 0;

            // Assert
            Assert.That(account.TimesLoggedIn, Is.EqualTo(0));
            Assert.That(account.HasCompletedTutorial, Is.False);
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(0));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(0));
            Assert.That(account.AchievementProgress.SkillsLearned, Is.EqualTo(0));
            Assert.That(account.AchievementProgress.QuestsCompleted, Is.EqualTo(0));
            Assert.That(account.AchievementProgress.ItemsCrafted, Is.EqualTo(0));
        }

        [Test]
        public void Account_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var account = new Account();

            // Act
            account.TimesLoggedIn = ulong.MaxValue;
            account.AchievementProgress.EnemiesKilled = ulong.MaxValue;
            account.AchievementProgress.PerksLearned = ulong.MaxValue;
            account.AchievementProgress.SkillsLearned = ulong.MaxValue;
            account.AchievementProgress.QuestsCompleted = ulong.MaxValue;
            account.AchievementProgress.ItemsCrafted = ulong.MaxValue;

            // Assert
            Assert.That(account.TimesLoggedIn, Is.EqualTo(ulong.MaxValue));
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(ulong.MaxValue));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(ulong.MaxValue));
            Assert.That(account.AchievementProgress.SkillsLearned, Is.EqualTo(ulong.MaxValue));
            Assert.That(account.AchievementProgress.QuestsCompleted, Is.EqualTo(ulong.MaxValue));
            Assert.That(account.AchievementProgress.ItemsCrafted, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void Account_WithMultipleAchievements_ShouldStoreMultipleAchievements()
        {
            // Arrange
            var account = new Account();
            var date1 = DateTime.Now.AddDays(-5);
            var date2 = DateTime.Now.AddDays(-3);
            var date3 = DateTime.Now.AddDays(-1);

            // Act
            account.Achievements[AchievementType.KillEnemies1] = date1;
            account.Achievements[AchievementType.CraftItems1] = date2;
            account.Achievements[AchievementType.CompleteQuests1] = date3;

            // Assert
            Assert.That(account.Achievements.Count, Is.EqualTo(3));
            Assert.That(account.Achievements[AchievementType.KillEnemies1], Is.EqualTo(date1));
            Assert.That(account.Achievements[AchievementType.CraftItems1], Is.EqualTo(date2));
            Assert.That(account.Achievements[AchievementType.CompleteQuests1], Is.EqualTo(date3));
        }

        [Test]
        public void Account_WithAchievementProgressIncrement_ShouldIncrementProgress()
        {
            // Arrange
            var account = new Account();

            // Act
            account.AchievementProgress.EnemiesKilled++;
            account.AchievementProgress.EnemiesKilled += 5;
            account.AchievementProgress.PerksLearned += 2;

            // Assert
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(6));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(2));
        }

        [Test]
        public void Account_WithAchievementProgressReset_ShouldResetProgress()
        {
            // Arrange
            var account = new Account();
            account.AchievementProgress.EnemiesKilled = 100;
            account.AchievementProgress.PerksLearned = 50;

            // Act
            account.AchievementProgress.EnemiesKilled = 0;
            account.AchievementProgress.PerksLearned = 0;

            // Assert
            Assert.That(account.AchievementProgress.EnemiesKilled, Is.EqualTo(0));
            Assert.That(account.AchievementProgress.PerksLearned, Is.EqualTo(0));
        }

        [Test]
        public void Account_WithAchievementRemoval_ShouldRemoveAchievement()
        {
            // Arrange
            var account = new Account();
            account.Achievements[AchievementType.KillEnemies1] = DateTime.Now;

            // Act
            account.Achievements.Remove(AchievementType.KillEnemies1);

            // Assert
            Assert.That(account.Achievements.Count, Is.EqualTo(0));
            Assert.That(account.Achievements.ContainsKey(AchievementType.KillEnemies1), Is.False);
        }

        [Test]
        public void Account_WithAchievementOverwrite_ShouldOverwriteAchievement()
        {
            // Arrange
            var account = new Account();
            var originalDate = DateTime.Now.AddDays(-5);
            var newDate = DateTime.Now;

            // Act
            account.Achievements[AchievementType.KillEnemies1] = originalDate;
            account.Achievements[AchievementType.KillEnemies1] = newDate;

            // Assert
            Assert.That(account.Achievements[AchievementType.KillEnemies1], Is.EqualTo(newDate));
        }

        [Test]
        public void Account_WithNullCdKey_ShouldHandleNullCdKey()
        {
            // Act
            var account = new Account(null);

            // Assert
            Assert.That(account.Id, Is.Null);
            Assert.That(account.TimesLoggedIn, Is.EqualTo(0));
            Assert.That(account.HasCompletedTutorial, Is.False);
        }

        [Test]
        public void Account_WithEmptyCdKey_ShouldHandleEmptyCdKey()
        {
            // Act
            var account = new Account("");

            // Assert
            Assert.That(account.Id, Is.EqualTo(""));
            Assert.That(account.TimesLoggedIn, Is.EqualTo(0));
            Assert.That(account.HasCompletedTutorial, Is.False);
        }

        [Test]
        public void Account_WithSpecialCharactersInCdKey_ShouldHandleSpecialCharacters()
        {
            // Arrange
            const string specialCdKey = "test-cd-key-!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var account = new Account(specialCdKey);

            // Assert
            Assert.That(account.Id, Is.EqualTo(specialCdKey));
        }

        [Test]
        public void Account_WithLongCdKey_ShouldHandleLongCdKey()
        {
            // Arrange
            var longCdKey = new string('a', 1000);

            // Act
            var account = new Account(longCdKey);

            // Assert
            Assert.That(account.Id, Is.EqualTo(longCdKey));
        }

        [Test]
        public void Account_WithAchievementProgressSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var account = new Account();
            account.AchievementProgress.EnemiesKilled = 100;
            account.AchievementProgress.PerksLearned = 25;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(account.AchievementProgress);
            var deserializedProgress = System.Text.Json.JsonSerializer.Deserialize<AchievementProgress>(json);

            // Assert
            Assert.That(deserializedProgress, Is.Not.Null);
            Assert.That(deserializedProgress.EnemiesKilled, Is.EqualTo(account.AchievementProgress.EnemiesKilled));
            Assert.That(deserializedProgress.PerksLearned, Is.EqualTo(account.AchievementProgress.PerksLearned));
        }

        [Test]
        public void Account_WithAchievementProgressEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var account1 = new Account();
            var account2 = new Account();
            account1.AchievementProgress.EnemiesKilled = 100;
            account2.AchievementProgress.EnemiesKilled = 100;

            // Act & Assert
            Assert.That(account1.AchievementProgress.EnemiesKilled, Is.EqualTo(account2.AchievementProgress.EnemiesKilled));
        }

        [Test]
        public void Account_WithAchievementProgressInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var account1 = new Account();
            var account2 = new Account();
            account1.AchievementProgress.EnemiesKilled = 100;
            account2.AchievementProgress.EnemiesKilled = 200;

            // Act & Assert
            Assert.That(account1.AchievementProgress.EnemiesKilled, Is.Not.EqualTo(account2.AchievementProgress.EnemiesKilled));
        }

        [Test]
        public void Account_WithAchievementProgressToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var account = new Account();
            account.AchievementProgress.EnemiesKilled = 100;

            // Act
            var result = account.AchievementProgress.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void Account_WithAchievementProgressGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var account = new Account();

            // Act
            var type = account.AchievementProgress.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(AchievementProgress)));
        }

        [Test]
        public void Account_WithAchievementProgressHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var account = new Account();
            account.AchievementProgress.EnemiesKilled = 100;

            // Act
            var hashCode = account.AchievementProgress.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }
    }
}
