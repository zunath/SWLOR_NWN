using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Tests.Feature;

public class RelocatedDungeonEnumMigrationTests
{
    // Values the renamed members held under their old names; a rename must never change a stored value.
    private static readonly Dictionary<string, int> NPCGroupValues = new()
    {
        ["Korriban_AbsoluteDefense_Adept"] = 102,
        ["Korriban_AbsoluteDefense_Specialist"] = 103,
        ["Korriban_AbsoluteDefense_Warden"] = 104,
        ["Korriban_AbsoluteDefense_InnerCircle"] = 105,
        ["Korriban_AbsoluteDefense_Master"] = 106,
        ["Korriban_SoulAscension_Adept"] = 107,
        ["Korriban_SoulAscension_Specialist"] = 108,
        ["Korriban_SoulAscension_Warden"] = 109,
        ["Korriban_SoulAscension_InnerCircle"] = 110,
        ["Korriban_SoulAscension_Master"] = 111,
        ["Korriban_Forcebane_Adept"] = 112,
        ["Korriban_Forcebane_Specialist"] = 113,
        ["Korriban_Forcebane_Warden"] = 114,
        ["Korriban_Forcebane_InnerCircle"] = 115,
        ["Korriban_Forcebane_Master"] = 116,
        ["Dantooine_HoldTheLine_Adept"] = 222,
        ["Dantooine_HoldTheLine_Specialist"] = 223,
        ["Dantooine_HoldTheLine_Warden"] = 224,
        ["Dantooine_HoldTheLine_InnerCircle"] = 225,
        ["Dantooine_HoldTheLine_Master"] = 226,
        ["Dantooine_EmergencyCocktail_Adept"] = 227,
        ["Dantooine_EmergencyCocktail_Specialist"] = 228,
        ["Dantooine_EmergencyCocktail_Warden"] = 229,
        ["Dantooine_EmergencyCocktail_InnerCircle"] = 230,
        ["Dantooine_EmergencyCocktail_Master"] = 231,
        ["Dantooine_InfiniteConduit_Adept"] = 232,
        ["Dantooine_InfiniteConduit_Specialist"] = 233,
        ["Dantooine_InfiniteConduit_Warden"] = 234,
        ["Dantooine_InfiniteConduit_InnerCircle"] = 235,
        ["Dantooine_InfiniteConduit_Master"] = 236,
        ["Dantooine_StormDrillDroid"] = 267,
        ["Dantooine_StormDuelistDroid"] = 268,
        ["Dantooine_StormGatekeeperDroid"] = 269,
        ["Dantooine_StormExaminerDroid"] = 270,
        ["Dantooine_TempestTrainingEngine"] = 271,
        ["Dantooine_GuardianPatrolDroid"] = 272,
        ["Dantooine_GuardianBulwarkDroid"] = 273,
        ["Dantooine_GuardianGatekeeperDroid"] = 274,
        ["Dantooine_GuardianShieldDroid"] = 275,
        ["Dantooine_BastionTrainingEngine"] = 276,
        ["Dantooine_CycloneDrillDroid"] = 277,
        ["Dantooine_CycloneDuelistDroid"] = 278,
        ["Dantooine_CycloneGatekeeperDroid"] = 279,
        ["Dantooine_CycloneExaminerDroid"] = 280,
        ["Dantooine_VortexTrainingEngine"] = 281,
        ["Korriban_CryptRaider"] = 282,
        ["Korriban_CryptMarauder"] = 283,
        ["Korriban_CryptBreachCaptain"] = 284,
        ["Korriban_CryptReaver"] = 285,
        ["Korriban_CryptExpeditionLeader"] = 286,
        ["Korriban_VitaeReaverInitiate"] = 287,
        ["Korriban_VitaeReaverAdept"] = 288,
        ["Korriban_VitaeReaverEnforcer"] = 289,
        ["Korriban_VitaeReaverBinder"] = 290,
        ["Korriban_VitaeReaverChief"] = 291,
        ["Korriban_UmbralRaiderScout"] = 292,
        ["Korriban_UmbralRaiderAdept"] = 293,
        ["Korriban_UmbralRaiderCaptain"] = 294,
        ["Korriban_UmbralRaiderSaboteur"] = 295,
        ["Korriban_UmbralRaiderCommander"] = 296,
    };

    private static readonly Dictionary<string, int> KeyItemValues = new()
    {
        ["CapstoneDantooineJediEnclaveTrialHallsKey"] = 108,
        ["CapstoneKorribanForgeCavernsKey"] = 109,
        ["CapstoneKorribanSithCryptDepthsKey"] = 115,
        ["CapstoneDantooineMedicalSublevelKey"] = 117,
    };

    [Test]
    public void Renames_KeepEveryStoredValueAndRetireTheOldNames()
    {
        RelocatedDungeonEnumMigration.NPCGroupRenames.Keys.Should().BeEquivalentTo(NPCGroupValues.Keys);
        RelocatedDungeonEnumMigration.KeyItemRenames.Keys.Should().BeEquivalentTo(KeyItemValues.Keys);
        foreach (var (oldName, value) in NPCGroupValues)
        {
            ((int)RelocatedDungeonEnumMigration.NPCGroupRenames[oldName]).Should().Be(value, oldName);
            Enum.TryParse<NPCGroupType>(oldName, out _).Should().BeFalse(oldName);
        }
        foreach (var (oldName, value) in KeyItemValues)
        {
            ((int)RelocatedDungeonEnumMigration.KeyItemRenames[oldName]).Should().Be(value, oldName);
            Enum.TryParse<KeyItemType>(oldName, out _).Should().BeFalse(oldName);
        }
    }

    [Test]
    public void StalePlayerJson_DeserializesAfterMigrationWithProgressPreserved()
    {
        var annexKey = (int)KeyItemType.CapstoneVelesMilitiaAnnexKey;
        var beacon = (int)NPCGroupType.Viscara_KillzoneBeacon_Adept;
        var player = JObject.Parse(
            "{\"Id\":\"relocated-player\"," +
            "\"KeyItems\":{\"CapstoneKorribanForgeCavernsKey\":\"2026-10-01T00:00:00Z\",\"" + annexKey + "\":\"2026-09-01T00:00:00Z\"," +
            "\"CapstoneMonCalaMedicalSublevelKey\":\"2026-08-01T00:00:00Z\",\"CapstoneDantooineMedicalSublevelKey\":\"2026-10-02T00:00:00Z\"}," +
            "\"Quests\":{" +
            "\"absolute_defense_foundation\":{\"CurrentState\":1,\"KillProgresses\":{\"Korriban_AbsoluteDefense_Adept\":4},\"ItemProgresses\":{}}," +
            "\"hold_the_line_circle\":{\"CurrentState\":1,\"KillProgresses\":{\"Dantooine_HoldTheLine_InnerCircle\":3,\"MonCala_HoldTheLine_InnerCircle\":2},\"ItemProgresses\":{}}," +
            "\"saber_storm_foundation\":{\"CurrentState\":1,\"KillProgresses\":{\"Dantooine_StormDrillDroid\":5,\"" + beacon + "\":1},\"ItemProgresses\":{}}}," +
            "\"UnknownField\":{\"value\":42}}");
        var unknown = player["UnknownField"]!.DeepClone();
        // A record that skipped migration 28 (e.g. restored afterwards) still loads, with the former names mapped.
        AssertMigrated(JsonConvert.DeserializeObject<Player>(player.ToString())!);

        RelocatedDungeonEnumMigration.MigratePlayer(player).Should().BeTrue();
        RelocatedDungeonEnumMigration.MigratePlayer(player).Should().BeFalse();
        JToken.DeepEquals(player["UnknownField"], unknown).Should().BeTrue();

        AssertMigrated(JsonConvert.DeserializeObject<Player>(player.ToString())!);
        var reserialized = JObject.Parse(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Player>(player.ToString())));
        reserialized[nameof(Player.KeyItems)]![nameof(KeyItemType.CapstoneTatooineForgeCavernsKey)].Should().NotBeNull();
    }

    private static void AssertMigrated(Player migrated)
    {
        migrated.KeyItems.Should().ContainKey(KeyItemType.CapstoneTatooineForgeCavernsKey);
        migrated.KeyItems.Should().ContainKey(KeyItemType.CapstoneVelesMilitiaAnnexKey);
        migrated.KeyItems[KeyItemType.CapstoneMonCalaMedicalSublevelKey].Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        migrated.Quests["absolute_defense_foundation"].KillProgresses[NPCGroupType.Tatooine_AbsoluteDefense_Adept].Should().Be(4);
        migrated.Quests["hold_the_line_circle"].KillProgresses.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<NPCGroupType, int>(NPCGroupType.MonCala_HoldTheLine_InnerCircle, 2));
        migrated.Quests["saber_storm_foundation"].KillProgresses[NPCGroupType.Hutlar_StormDrillDroid].Should().Be(5);
        migrated.Quests["saber_storm_foundation"].KillProgresses[NPCGroupType.Viscara_KillzoneBeacon_Adept].Should().Be(1);
    }

    [Test]
    public void Migration_RunsOnRawJsonBeforeTypedMigrations()
    {
        var migration = new _28_RenameRelocatedDungeonEnums();
        migration.ExecutionType.Should().Be(MigrationExecutionType.PostDatabaseLoad);
        migration.Version.Should().Be(28);
    }
}
