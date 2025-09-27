using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Test.Shared.Core.TestHelpers;

namespace SWLOR.Test.Shared.Domain.ValueObjects
{
    [TestFixture]
    public class PlayerQuestTests
    {
        [Test]
        public void PlayerQuest_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var playerQuest = new PlayerQuest();

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(0));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(0));
            Assert.That(playerQuest.DateLastCompleted, Is.Null);
            Assert.That(playerQuest.KillProgresses, Is.Not.Null);
            Assert.That(playerQuest.ItemProgresses, Is.Not.Null);
        }

        [Test]
        public void PlayerQuest_WithCurrentState_ShouldStoreCurrentStateCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = 3;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(3));
        }

        [Test]
        public void PlayerQuest_WithTimesCompleted_ShouldStoreTimesCompletedCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.TimesCompleted = 5;

            // Assert
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(5));
        }

        [Test]
        public void PlayerQuest_WithDateLastCompleted_ShouldStoreDateLastCompletedCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();
            var completionDate = DateTime.Now.AddDays(-1);

            // Act
            playerQuest.DateLastCompleted = completionDate;

            // Assert
            Assert.That(playerQuest.DateLastCompleted, Is.EqualTo(completionDate));
        }

        [Test]
        public void PlayerQuest_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();
            var completionDate = DateTime.Now.AddDays(-2);

            // Act
            playerQuest.CurrentState = 4;
            playerQuest.TimesCompleted = 3;
            playerQuest.DateLastCompleted = completionDate;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(4));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(3));
            Assert.That(playerQuest.DateLastCompleted, Is.EqualTo(completionDate));
        }

        [Test]
        public void PlayerQuest_WithKillProgresses_ShouldStoreKillProgressesCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks] = 5;
            playerQuest.KillProgresses[NPCGroupType.CZ220_MalfunctioningDroids] = 3;
            playerQuest.KillProgresses[NPCGroupType.Viscara_WildlandKathHounds] = 2;

            // Assert
            Assert.That(playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks], Is.EqualTo(5));
            Assert.That(playerQuest.KillProgresses[NPCGroupType.CZ220_MalfunctioningDroids], Is.EqualTo(3));
            Assert.That(playerQuest.KillProgresses[NPCGroupType.Viscara_WildlandKathHounds], Is.EqualTo(2));
        }

        [Test]
        public void PlayerQuest_WithItemProgresses_ShouldStoreItemProgressesCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.ItemProgresses["item_1"] = 10;
            playerQuest.ItemProgresses["item_2"] = 5;
            playerQuest.ItemProgresses["item_3"] = 1;

            // Assert
            Assert.That(playerQuest.ItemProgresses["item_1"], Is.EqualTo(10));
            Assert.That(playerQuest.ItemProgresses["item_2"], Is.EqualTo(5));
            Assert.That(playerQuest.ItemProgresses["item_3"], Is.EqualTo(1));
        }

        [Test]
        public void PlayerQuest_WithNegativeValues_ShouldStoreNegativeValues()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = -1;
            playerQuest.TimesCompleted = -5;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(-1));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(-5));
        }

        [Test]
        public void PlayerQuest_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = 0;
            playerQuest.TimesCompleted = 0;
            playerQuest.DateLastCompleted = null;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(0));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(0));
            Assert.That(playerQuest.DateLastCompleted, Is.Null);
        }

        [Test]
        public void PlayerQuest_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = 100;
            playerQuest.TimesCompleted = 999;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(100));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(999));
        }

        [Test]
        public void PlayerQuest_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = int.MaxValue;
            playerQuest.TimesCompleted = int.MaxValue;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(int.MaxValue));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void PlayerQuest_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            playerQuest.CurrentState = int.MinValue;
            playerQuest.TimesCompleted = int.MinValue;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(int.MinValue));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void PlayerQuest_WithQuestProgression_ShouldSimulateQuestProgression()
        {
            // Arrange
            var playerQuest = new PlayerQuest { CurrentState = 1, TimesCompleted = 0 };

            // Act - Progress through quest states
            playerQuest.CurrentState = 2;
            playerQuest.CurrentState = 3;
            playerQuest.CurrentState = 4;
            playerQuest.TimesCompleted = 1;
            playerQuest.DateLastCompleted = DateTime.Now;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(4));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(1));
            Assert.That(playerQuest.DateLastCompleted, Is.Not.Null);
        }

        [Test]
        public void PlayerQuest_WithQuestReset_ShouldSimulateQuestReset()
        {
            // Arrange
            var playerQuest = new PlayerQuest 
            { 
                CurrentState = 5, 
                TimesCompleted = 3, 
                DateLastCompleted = DateTime.Now.AddDays(-1) 
            };

            // Act - Reset quest
            playerQuest.CurrentState = 1;
            playerQuest.TimesCompleted = 0;
            playerQuest.DateLastCompleted = null;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(1));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(0));
            Assert.That(playerQuest.DateLastCompleted, Is.Null);
        }

        [Test]
        public void PlayerQuest_WithQuestCompletion_ShouldSimulateQuestCompletion()
        {
            // Arrange
            var playerQuest = new PlayerQuest { CurrentState = 4, TimesCompleted = 2 };

            // Act - Complete quest
            playerQuest.CurrentState = 5;
            playerQuest.TimesCompleted++;
            playerQuest.DateLastCompleted = DateTime.Now;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(5));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(3));
            Assert.That(playerQuest.DateLastCompleted, Is.Not.Null);
        }

        [Test]
        public void PlayerQuest_WithKillProgressTracking_ShouldTrackKillProgress()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act - Track kill progress
            playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks] = 0;
            playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks] += 1;
            playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks] += 2;
            playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks] += 3;

            // Assert
            Assert.That(playerQuest.KillProgresses[NPCGroupType.CZ220_Mynocks], Is.EqualTo(6));
        }

        [Test]
        public void PlayerQuest_WithItemProgressTracking_ShouldTrackItemProgress()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act - Track item progress
            playerQuest.ItemProgresses["item_1"] = 0;
            playerQuest.ItemProgresses["item_1"] += 5;
            playerQuest.ItemProgresses["item_1"] += 3;
            playerQuest.ItemProgresses["item_1"] += 2;

            // Assert
            Assert.That(playerQuest.ItemProgresses["item_1"], Is.EqualTo(10));
        }

        [Test]
        public void PlayerQuest_WithQuestComparison_ShouldCompareQuestsCorrectly()
        {
            // Arrange
            var playerQuest1 = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };
            var playerQuest2 = new PlayerQuest { CurrentState = 3, TimesCompleted = 1 };
            var playerQuest3 = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };

            // Act & Assert
            Assert.That(playerQuest1.CurrentState, Is.GreaterThan(playerQuest2.CurrentState));
            Assert.That(playerQuest1.TimesCompleted, Is.GreaterThan(playerQuest2.TimesCompleted));
            Assert.That(playerQuest1.CurrentState, Is.EqualTo(playerQuest3.CurrentState));
            Assert.That(playerQuest1.TimesCompleted, Is.EqualTo(playerQuest3.TimesCompleted));
        }

        [Test]
        public void PlayerQuest_WithQuestValidation_ShouldValidateQuestsCorrectly()
        {
            // Arrange
            var validQuest = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };
            var invalidQuest = new PlayerQuest { CurrentState = -1, TimesCompleted = -5 };

            // Act & Assert
            Assert.That(validQuest.CurrentState, Is.GreaterThanOrEqualTo(0));
            Assert.That(validQuest.TimesCompleted, Is.GreaterThanOrEqualTo(0));
            Assert.That(invalidQuest.CurrentState, Is.LessThan(0));
            Assert.That(invalidQuest.TimesCompleted, Is.LessThan(0));
        }

        [Test]
        public void PlayerQuest_WithQuestEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var playerQuest1 = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };
            var playerQuest2 = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };
            var playerQuest3 = new PlayerQuest { CurrentState = 5, TimesCompleted = 2 };

            // Act & Assert
            Assert.That(playerQuest1.CurrentState, Is.EqualTo(playerQuest2.CurrentState));
            Assert.That(playerQuest1.TimesCompleted, Is.EqualTo(playerQuest2.TimesCompleted));
            Assert.That(playerQuest1.TimesCompleted, Is.Not.EqualTo(playerQuest3.TimesCompleted));
        }

        [Test]
        public void PlayerQuest_WithQuestToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var playerQuest = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };

            // Act
            var result = playerQuest.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void PlayerQuest_WithQuestHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var playerQuest = new PlayerQuest { CurrentState = 5, TimesCompleted = 3 };

            // Act
            var hashCode = playerQuest.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }


        [Test]
        public void PlayerQuest_WithQuestGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var playerQuest = new PlayerQuest();

            // Act
            var type = playerQuest.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(PlayerQuest)));
        }


        [Test]
        public void PlayerQuest_WithQuestSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var playerQuest = new PlayerQuest 
            { 
                CurrentState = 5, 
                TimesCompleted = 3, 
                DateLastCompleted = DateTime.Now 
            };

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(playerQuest);
            var deserializedQuest = System.Text.Json.JsonSerializer.Deserialize<PlayerQuest>(json);

            // Assert
            Assert.That(deserializedQuest, Is.Not.Null);
            Assert.That(deserializedQuest.CurrentState, Is.EqualTo(playerQuest.CurrentState));
            Assert.That(deserializedQuest.TimesCompleted, Is.EqualTo(playerQuest.TimesCompleted));
            Assert.That(deserializedQuest.DateLastCompleted, Is.EqualTo(playerQuest.DateLastCompleted));
        }

        [Test]
        public void PlayerQuest_WithQuestInPlayer_ShouldWorkWithPlayer()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var questDate = DateTime.Now.AddDays(-1);

            // Act
            player.Quests["test_quest_1"] = new PlayerQuest 
            { 
                CurrentState = 1, 
                TimesCompleted = 0,
                DateLastCompleted = null
            };
            
            player.Quests["test_quest_2"] = new PlayerQuest 
            { 
                CurrentState = 3, 
                TimesCompleted = 1,
                DateLastCompleted = questDate
            };

            // Assert
            Assert.That(player.Quests["test_quest_1"].CurrentState, Is.EqualTo(1));
            Assert.That(player.Quests["test_quest_1"].TimesCompleted, Is.EqualTo(0));
            Assert.That(player.Quests["test_quest_1"].DateLastCompleted, Is.Null);

            Assert.That(player.Quests["test_quest_2"].CurrentState, Is.EqualTo(3));
            Assert.That(player.Quests["test_quest_2"].TimesCompleted, Is.EqualTo(1));
            Assert.That(player.Quests["test_quest_2"].DateLastCompleted, Is.EqualTo(questDate));
        }

        [Test]
        public void PlayerQuest_WithQuestStateTransitions_ShouldHandleStateTransitions()
        {
            // Arrange
            var playerQuest = new PlayerQuest { CurrentState = 1, TimesCompleted = 0 };

            // Act - Progress through states
            playerQuest.CurrentState = 2; // Started
            playerQuest.CurrentState = 3; // In Progress
            playerQuest.CurrentState = 4; // Almost Complete
            playerQuest.CurrentState = 5; // Complete
            playerQuest.TimesCompleted = 1;
            playerQuest.DateLastCompleted = DateTime.Now;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(5));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(1));
            Assert.That(playerQuest.DateLastCompleted, Is.Not.Null);
        }

        [Test]
        public void PlayerQuest_WithQuestReplay_ShouldHandleQuestReplay()
        {
            // Arrange
            var playerQuest = new PlayerQuest 
            { 
                CurrentState = 5, 
                TimesCompleted = 2, 
                DateLastCompleted = DateTime.Now.AddDays(-1) 
            };

            // Act - Replay quest
            playerQuest.CurrentState = 1;
            playerQuest.CurrentState = 2;
            playerQuest.CurrentState = 3;
            playerQuest.CurrentState = 4;
            playerQuest.CurrentState = 5;
            playerQuest.TimesCompleted = 3;
            playerQuest.DateLastCompleted = DateTime.Now;

            // Assert
            Assert.That(playerQuest.CurrentState, Is.EqualTo(5));
            Assert.That(playerQuest.TimesCompleted, Is.EqualTo(3));
            Assert.That(playerQuest.DateLastCompleted, Is.Not.Null);
        }
    }
}
