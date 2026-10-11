using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonEnemyGroupMigrationTests
{
    [TestCase("Dantooine_SaberStorm_Adept", 87, NPCGroupType.Dantooine_StormDrillDroid)]
    [TestCase("Dantooine_SaberStorm_Specialist", 88, NPCGroupType.Dantooine_StormDuelistDroid)]
    [TestCase("Dantooine_SaberStorm_Warden", 89, NPCGroupType.Dantooine_StormGatekeeperDroid)]
    [TestCase("Dantooine_SaberStorm_InnerCircle", 90, NPCGroupType.Dantooine_StormExaminerDroid)]
    [TestCase("Dantooine_SaberStorm_Master", 91, NPCGroupType.Dantooine_TempestTrainingEngine)]
    [TestCase("Dantooine_GuardianMaster_Adept", 92, NPCGroupType.Dantooine_GuardianPatrolDroid)]
    [TestCase("Dantooine_GuardianMaster_Specialist", 93, NPCGroupType.Dantooine_GuardianBulwarkDroid)]
    [TestCase("Dantooine_GuardianMaster_Warden", 94, NPCGroupType.Dantooine_GuardianGatekeeperDroid)]
    [TestCase("Dantooine_GuardianMaster_InnerCircle", 95, NPCGroupType.Dantooine_GuardianShieldDroid)]
    [TestCase("Dantooine_GuardianMaster_Paragon", 96, NPCGroupType.Dantooine_BastionTrainingEngine)]
    [TestCase("Dantooine_SaberCyclone_Adept", 97, NPCGroupType.Dantooine_CycloneDrillDroid)]
    [TestCase("Dantooine_SaberCyclone_Specialist", 98, NPCGroupType.Dantooine_CycloneDuelistDroid)]
    [TestCase("Dantooine_SaberCyclone_Warden", 99, NPCGroupType.Dantooine_CycloneGatekeeperDroid)]
    [TestCase("Dantooine_SaberCyclone_InnerCircle", 100, NPCGroupType.Dantooine_CycloneExaminerDroid)]
    [TestCase("Dantooine_SaberCyclone_Master", 101, NPCGroupType.Dantooine_VortexTrainingEngine)]
    [TestCase("Korriban_LastStandOfTheLight_Adept", 192, NPCGroupType.Korriban_CryptRaider)]
    [TestCase("Korriban_LastStandOfTheLight_Specialist", 193, NPCGroupType.Korriban_CryptMarauder)]
    [TestCase("Korriban_LastStandOfTheLight_Warden", 194, NPCGroupType.Korriban_CryptBreachCaptain)]
    [TestCase("Korriban_LastStandOfTheLight_InnerCircle", 195, NPCGroupType.Korriban_CryptReaver)]
    [TestCase("Korriban_LastStandOfTheLight_Master", 196, NPCGroupType.Korriban_CryptExpeditionLeader)]
    [TestCase("Korriban_HungerOfTheDark_Adept", 197, NPCGroupType.Korriban_VitaeReaverInitiate)]
    [TestCase("Korriban_HungerOfTheDark_Specialist", 198, NPCGroupType.Korriban_VitaeReaverAdept)]
    [TestCase("Korriban_HungerOfTheDark_Warden", 199, NPCGroupType.Korriban_VitaeReaverEnforcer)]
    [TestCase("Korriban_HungerOfTheDark_InnerCircle", 200, NPCGroupType.Korriban_VitaeReaverBinder)]
    [TestCase("Korriban_HungerOfTheDark_Master", 201, NPCGroupType.Korriban_VitaeReaverChief)]
    [TestCase("Korriban_EclipseOfResolve_Adept", 202, NPCGroupType.Korriban_UmbralRaiderScout)]
    [TestCase("Korriban_EclipseOfResolve_Specialist", 203, NPCGroupType.Korriban_UmbralRaiderAdept)]
    [TestCase("Korriban_EclipseOfResolve_Warden", 204, NPCGroupType.Korriban_UmbralRaiderCaptain)]
    [TestCase("Korriban_EclipseOfResolve_InnerCircle", 205, NPCGroupType.Korriban_UmbralRaiderSaboteur)]
    [TestCase("Korriban_EclipseOfResolve_Master", 206, NPCGroupType.Korriban_UmbralRaiderCommander)]
    public void LegacyNamedAndNumericKeysLoadAndMigrateWithoutLosingQuestState(string legacyName, int legacyId, NPCGroupType replacement)
    {
        ((int)Enum.Parse<NPCGroupType>(legacyName)).Should().Be(legacyId,
            "existing names and values must remain available to read historical saves");
        ((int)replacement).Should().BeGreaterThan(266);
        foreach (var key in new[] { legacyName, legacyId.ToString() })
        {
            var source = new JObject
            {
                ["Quests"] = new JObject
                {
                    ["active"] = new JObject
                    {
                        ["CurrentState"] = 1, ["TimesCompleted"] = 2,
                        ["DateLastCompleted"] = "2026-10-01T12:00:00Z",
                        ["KillProgresses"] = new JObject { [key] = 3, ["CZ220_Mynocks"] = 5 },
                        ["ItemProgresses"] = new JObject { ["retained-proof"] = 1 }
                    }
                }
            };
            var player = JsonConvert.DeserializeObject<Player>(source.ToString())!;
            var quest = player.Quests["active"];
            var completion = quest.DateLastCompleted;
            DungeonEnemyGroupMigration.MigrateQuestProgress(player).Should().BeTrue();
            quest.KillProgresses.Should().BeEquivalentTo(new Dictionary<NPCGroupType, int>
            {
                [replacement] = 3, [NPCGroupType.CZ220_Mynocks] = 5
            });
            quest.CurrentState.Should().Be(1);
            quest.TimesCompleted.Should().Be(2);
            quest.DateLastCompleted.Should().Be(completion);
            quest.ItemProgresses.Should().Contain("retained-proof", 1);
            var saved = JsonConvert.SerializeObject(player);
            saved.Should().Contain(replacement.ToString()).And.NotContain(legacyName);
            DungeonEnemyGroupMigration.MigrateQuestProgress(player).Should().BeFalse();
            JsonConvert.SerializeObject(player).Should().Be(saved);
        }
    }

    [TestCase(5, 2, 2)]
    [TestCase(2, 5, 2)]
    [TestCase(0, 4, 0)]
    [TestCase(4, 0, 0)]
    public void MixedOldAndNewCountersRetainTheMostProgress(int oldRemaining, int newRemaining, int expected)
    {
        var player = new Player("mixed-enemy-groups");
        player.Quests["saber_storm_foundation"] = new PlayerQuest
        {
            CurrentState = 2,
            KillProgresses = new Dictionary<NPCGroupType, int>
            {
                [NPCGroupType.Dantooine_SaberStorm_Adept] = oldRemaining,
                [NPCGroupType.Dantooine_StormDrillDroid] = newRemaining
            }
        };
        DungeonEnemyGroupMigration.MigrateQuestProgress(player).Should().BeTrue();
        var quest = player.Quests["saber_storm_foundation"];
        quest.KillProgresses.Should().ContainSingle().Which.Should().Be(
            new KeyValuePair<NPCGroupType, int>(NPCGroupType.Dantooine_StormDrillDroid, expected));
        quest.CurrentState.Should().Be(2);
    }

    [Test]
    public void ReplacementGroupsHaveDistinctIdsAndOnlyLegacyGroupsChange()
    {
        var groups = Enum.GetValues<NPCGroupType>();
        groups.Should().OnlyHaveUniqueItems();
        groups.Count(group => DungeonEnemyGroupMigration.GetReplacement(group) != group).Should().Be(30);
        foreach (var group in groups)
        {
            var replacement = DungeonEnemyGroupMigration.GetReplacement(group);
            DungeonEnemyGroupMigration.GetReplacement(replacement).Should().Be(replacement);
        }
        new _26_ReplaceDungeonEnemyGroups().Version.Should().Be(26);
        new _26_ReplaceDungeonEnemyGroups().ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
    }
}
