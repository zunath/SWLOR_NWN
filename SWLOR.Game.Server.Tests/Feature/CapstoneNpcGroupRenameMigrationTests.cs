using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneNpcGroupRenameMigrationTests
{
    // Values the renamed members held under their old names. A kept member keeps its value; the other two lines'
    // members of a dungeon tier merge into the first line's member, which holds the lowest value.
    private static readonly Dictionary<string, int> FormerValues = new()
    {
        ["Viscara_Invincible_Adept"] = 72,
        ["Viscara_Invincible_Specialist"] = 73,
        ["Viscara_Invincible_Warden"] = 74,
        ["Viscara_Invincible_InnerCircle"] = 75,
        ["Viscara_Invincible_Master"] = 76,
        ["Viscara_VitalRupture_Adept"] = 77,
        ["Viscara_VitalRupture_Specialist"] = 78,
        ["Viscara_VitalRupture_Warden"] = 79,
        ["Viscara_VitalRupture_InnerCircle"] = 80,
        ["Viscara_VitalRupture_Master"] = 81,
        ["Viscara_SystemicShutdown_Adept"] = 82,
        ["Viscara_SystemicShutdown_Specialist"] = 83,
        ["Viscara_SystemicShutdown_Warden"] = 84,
        ["Viscara_SystemicShutdown_InnerCircle"] = 85,
        ["Viscara_SystemicShutdown_Master"] = 86,
        ["Tatooine_AbsoluteDefense_Adept"] = 102,
        ["Tatooine_AbsoluteDefense_Specialist"] = 103,
        ["Tatooine_AbsoluteDefense_Warden"] = 104,
        ["Tatooine_AbsoluteDefense_InnerCircle"] = 105,
        ["Tatooine_AbsoluteDefense_Master"] = 106,
        ["Tatooine_SoulAscension_Adept"] = 107,
        ["Tatooine_SoulAscension_Specialist"] = 108,
        ["Tatooine_SoulAscension_Warden"] = 109,
        ["Tatooine_SoulAscension_InnerCircle"] = 110,
        ["Tatooine_SoulAscension_Master"] = 111,
        ["Tatooine_Forcebane_Adept"] = 112,
        ["Tatooine_Forcebane_Specialist"] = 113,
        ["Tatooine_Forcebane_Warden"] = 114,
        ["Tatooine_Forcebane_InnerCircle"] = 115,
        ["Tatooine_Forcebane_Master"] = 116,
        ["SmugglersMoon_CripplingDefense_Adept"] = 117,
        ["SmugglersMoon_CripplingDefense_Specialist"] = 118,
        ["SmugglersMoon_CripplingDefense_Warden"] = 119,
        ["SmugglersMoon_CripplingDefense_InnerCircle"] = 120,
        ["SmugglersMoon_CripplingDefense_Master"] = 121,
        ["SmugglersMoon_TempestBloom_Adept"] = 122,
        ["SmugglersMoon_TempestBloom_Specialist"] = 123,
        ["SmugglersMoon_TempestBloom_Warden"] = 124,
        ["SmugglersMoon_TempestBloom_InnerCircle"] = 125,
        ["SmugglersMoon_TempestBloom_Master"] = 126,
        ["SmugglersMoon_RedBloom_Adept"] = 127,
        ["SmugglersMoon_RedBloom_Specialist"] = 128,
        ["SmugglersMoon_RedBloom_Warden"] = 129,
        ["SmugglersMoon_RedBloom_InnerCircle"] = 130,
        ["SmugglersMoon_RedBloom_Master"] = 131,
        ["CZ220_AdamantineGuard_Adept"] = 132,
        ["CZ220_AdamantineGuard_Specialist"] = 133,
        ["CZ220_AdamantineGuard_Warden"] = 134,
        ["CZ220_AdamantineGuard_InnerCircle"] = 135,
        ["CZ220_AdamantineGuard_Master"] = 136,
        ["CZ220_ScrapheapLockdown_Adept"] = 137,
        ["CZ220_ScrapheapLockdown_Specialist"] = 138,
        ["CZ220_ScrapheapLockdown_Warden"] = 139,
        ["CZ220_ScrapheapLockdown_InnerCircle"] = 140,
        ["CZ220_ScrapheapLockdown_Master"] = 141,
        ["CZ220_Worldbreaker_Adept"] = 142,
        ["CZ220_Worldbreaker_Specialist"] = 143,
        ["CZ220_Worldbreaker_Warden"] = 144,
        ["CZ220_Worldbreaker_InnerCircle"] = 145,
        ["CZ220_Worldbreaker_Master"] = 146,
        ["Tatooine_UnmovingCenter_Adept"] = 147,
        ["Tatooine_UnmovingCenter_Specialist"] = 148,
        ["Tatooine_UnmovingCenter_Warden"] = 149,
        ["Tatooine_UnmovingCenter_InnerCircle"] = 150,
        ["Tatooine_UnmovingCenter_Master"] = 151,
        ["Tatooine_LastWord_Adept"] = 152,
        ["Tatooine_LastWord_Specialist"] = 153,
        ["Tatooine_LastWord_Warden"] = 154,
        ["Tatooine_LastWord_InnerCircle"] = 155,
        ["Tatooine_LastWord_Master"] = 156,
        ["Tatooine_DeadMansHand_Adept"] = 157,
        ["Tatooine_DeadMansHand_Specialist"] = 158,
        ["Tatooine_DeadMansHand_Warden"] = 159,
        ["Tatooine_DeadMansHand_InnerCircle"] = 160,
        ["Tatooine_DeadMansHand_Master"] = 161,
        ["SmugglersMoon_KillBox_Adept"] = 162,
        ["SmugglersMoon_KillBox_Specialist"] = 163,
        ["SmugglersMoon_KillBox_Warden"] = 164,
        ["SmugglersMoon_KillBox_InnerCircle"] = 165,
        ["SmugglersMoon_KillBox_Master"] = 166,
        ["SmugglersMoon_OneShot_Adept"] = 167,
        ["SmugglersMoon_OneShot_Specialist"] = 168,
        ["SmugglersMoon_OneShot_Warden"] = 169,
        ["SmugglersMoon_OneShot_InnerCircle"] = 170,
        ["SmugglersMoon_OneShot_Master"] = 171,
        ["SmugglersMoon_RainOfSteel_Adept"] = 172,
        ["SmugglersMoon_RainOfSteel_Specialist"] = 173,
        ["SmugglersMoon_RainOfSteel_Warden"] = 174,
        ["SmugglersMoon_RainOfSteel_InnerCircle"] = 175,
        ["SmugglersMoon_RainOfSteel_Master"] = 176,
        ["Hutlar_PerfectFlurry_Adept"] = 177,
        ["Hutlar_PerfectFlurry_Specialist"] = 178,
        ["Hutlar_PerfectFlurry_Warden"] = 179,
        ["Hutlar_PerfectFlurry_InnerCircle"] = 180,
        ["Hutlar_PerfectFlurry_Master"] = 181,
        ["Hutlar_ThermalDetonator_Adept"] = 182,
        ["Hutlar_ThermalDetonator_Specialist"] = 183,
        ["Hutlar_ThermalDetonator_Warden"] = 184,
        ["Hutlar_ThermalDetonator_InnerCircle"] = 185,
        ["Hutlar_ThermalDetonator_Master"] = 186,
        ["Hutlar_OverloadBarrage_Adept"] = 187,
        ["Hutlar_OverloadBarrage_Specialist"] = 188,
        ["Hutlar_OverloadBarrage_Warden"] = 189,
        ["Hutlar_OverloadBarrage_InnerCircle"] = 190,
        ["Hutlar_OverloadBarrage_Master"] = 191,
        ["Viscara_KillzoneBeacon_Adept"] = 207,
        ["Viscara_KillzoneBeacon_Specialist"] = 208,
        ["Viscara_KillzoneBeacon_Warden"] = 209,
        ["Viscara_KillzoneBeacon_InnerCircle"] = 210,
        ["Viscara_KillzoneBeacon_Master"] = 211,
        ["Viscara_EmergencyBunker_Adept"] = 212,
        ["Viscara_EmergencyBunker_Specialist"] = 213,
        ["Viscara_EmergencyBunker_Warden"] = 214,
        ["Viscara_EmergencyBunker_InnerCircle"] = 215,
        ["Viscara_EmergencyBunker_Master"] = 216,
        ["Viscara_DecisiveCommand_Adept"] = 217,
        ["Viscara_DecisiveCommand_Specialist"] = 218,
        ["Viscara_DecisiveCommand_Warden"] = 219,
        ["Viscara_DecisiveCommand_InnerCircle"] = 220,
        ["Viscara_DecisiveCommand_Master"] = 221,
        ["MonCala_HoldTheLine_Adept"] = 222,
        ["MonCala_HoldTheLine_Specialist"] = 223,
        ["MonCala_HoldTheLine_Warden"] = 224,
        ["MonCala_HoldTheLine_InnerCircle"] = 225,
        ["MonCala_HoldTheLine_Master"] = 226,
        ["MonCala_EmergencyCocktail_Adept"] = 227,
        ["MonCala_EmergencyCocktail_Specialist"] = 228,
        ["MonCala_EmergencyCocktail_Warden"] = 229,
        ["MonCala_EmergencyCocktail_InnerCircle"] = 230,
        ["MonCala_EmergencyCocktail_Master"] = 231,
        ["MonCala_InfiniteConduit_Adept"] = 232,
        ["MonCala_InfiniteConduit_Specialist"] = 233,
        ["MonCala_InfiniteConduit_Warden"] = 234,
        ["MonCala_InfiniteConduit_InnerCircle"] = 235,
        ["MonCala_InfiniteConduit_Master"] = 236,
        ["Dathomir_ApexBite_Adept"] = 237,
        ["Dathomir_ApexBite_Specialist"] = 238,
        ["Dathomir_ApexBite_Warden"] = 239,
        ["Dathomir_ApexBite_InnerCircle"] = 240,
        ["Dathomir_ApexBite_Master"] = 241,
        ["Dathomir_UnbreakableBeast_Adept"] = 242,
        ["Dathomir_UnbreakableBeast_Specialist"] = 243,
        ["Dathomir_UnbreakableBeast_Warden"] = 244,
        ["Dathomir_UnbreakableBeast_InnerCircle"] = 245,
        ["Dathomir_UnbreakableBeast_Master"] = 246,
        ["Dathomir_AlphaRhythm_Adept"] = 247,
        ["Dathomir_AlphaRhythm_Specialist"] = 248,
        ["Dathomir_AlphaRhythm_Warden"] = 249,
        ["Dathomir_AlphaRhythm_InnerCircle"] = 250,
        ["Dathomir_AlphaRhythm_Master"] = 251,
        ["Dathomir_PrimalOverrun_Adept"] = 252,
        ["Dathomir_PrimalOverrun_Specialist"] = 253,
        ["Dathomir_PrimalOverrun_Warden"] = 254,
        ["Dathomir_PrimalOverrun_InnerCircle"] = 255,
        ["Dathomir_PrimalOverrun_Master"] = 256,
        ["Dathomir_UntouchableInstinct_Adept"] = 257,
        ["Dathomir_UntouchableInstinct_Specialist"] = 258,
        ["Dathomir_UntouchableInstinct_Warden"] = 259,
        ["Dathomir_UntouchableInstinct_InnerCircle"] = 260,
        ["Dathomir_UntouchableInstinct_Master"] = 261,
        ["Dathomir_ForceBondedBeast_Adept"] = 262,
        ["Dathomir_ForceBondedBeast_Specialist"] = 263,
        ["Dathomir_ForceBondedBeast_Warden"] = 264,
        ["Dathomir_ForceBondedBeast_InnerCircle"] = 265,
        ["Dathomir_ForceBondedBeast_Master"] = 266,
        ["Hutlar_StormDrillDroid"] = 267,
        ["Hutlar_StormDuelistDroid"] = 268,
        ["Hutlar_StormGatekeeperDroid"] = 269,
        ["Hutlar_StormExaminerDroid"] = 270,
        ["Hutlar_GuardianPatrolDroid"] = 272,
        ["Hutlar_GuardianBulwarkDroid"] = 273,
        ["Hutlar_GuardianGatekeeperDroid"] = 274,
        ["Hutlar_GuardianShieldDroid"] = 275,
        ["Hutlar_CycloneDrillDroid"] = 277,
        ["Hutlar_CycloneDuelistDroid"] = 278,
        ["Hutlar_CycloneGatekeeperDroid"] = 279,
        ["Hutlar_CycloneExaminerDroid"] = 280,
        ["MonCala_CryptRaider"] = 282,
        ["MonCala_CryptMarauder"] = 283,
        ["MonCala_CryptBreachCaptain"] = 284,
        ["MonCala_CryptReaver"] = 285,
        ["MonCala_CryptExpeditionLeader"] = 286,
        ["MonCala_VitaeReaverInitiate"] = 287,
        ["MonCala_VitaeReaverAdept"] = 288,
        ["MonCala_VitaeReaverEnforcer"] = 289,
        ["MonCala_VitaeReaverBinder"] = 290,
        ["MonCala_VitaeReaverChief"] = 291,
        ["MonCala_UmbralRaiderScout"] = 292,
        ["MonCala_UmbralRaiderAdept"] = 293,
        ["MonCala_UmbralRaiderCaptain"] = 294,
        ["MonCala_UmbralRaiderSaboteur"] = 295,
        ["MonCala_UmbralRaiderCommander"] = 296,
    };

    [Test]
    public void Renames_KeepTheFirstLinesValueAndRetireTheOldNames()
    {
        CapstoneNpcGroupRenameMigration.Renames.Keys.Should().BeEquivalentTo(FormerValues.Keys);
        foreach (var group in FormerValues.GroupBy(former => CapstoneNpcGroupRenameMigration.Renames[former.Key]))
        {
            ((int)group.Key).Should().Be(group.Min(former => former.Value), group.Key.ToString());
            group.Count().Should().BeOneOf(new[] { 1, 3 }, $"{group.Key} is one line's group or a shared dungeon tier");
        }
        foreach (var (oldName, value) in FormerValues)
        {
            Enum.TryParse<NPCGroupType>(oldName, out _).Should().BeFalse(oldName);
            if ((int)CapstoneNpcGroupRenameMigration.Renames[oldName] != value)
                Enum.IsDefined(typeof(NPCGroupType), value).Should().BeFalse($"{oldName}'s value {value} merged away");
        }
    }

    [Test]
    public void StalePlayerJson_KeepsKillProgressUnderTheTiersGroup()
    {
        // Two quests mid-hunt: Primal Overrun's adept step under its own former group, and Untouchable Instinct's under
        // a group merged into the same tier. A third quest already uses the new name and keeps its count.
        var player = JObject.Parse(
            "{\"Id\":\"capstone-group-player\",\"Quests\":{" +
            "\"cap_primal_adept\":{\"CurrentState\":1,\"KillProgresses\":{\"Dathomir_PrimalOverrun_Adept\":4}}," +
            "\"cap_untouch_adept\":{\"CurrentState\":1,\"KillProgresses\":{\"Dathomir_UntouchableInstinct_Adept\":2}}," +
            "\"cap_alpha_adept\":{\"CurrentState\":1,\"KillProgresses\":{\"Dathomir_YoungGrottoBeasts\":1,\"Dathomir_PrimalOverrun_Adept\":6}}," +
            "\"other_quest\":{\"CurrentState\":2,\"KillProgresses\":{\"Dathomir_Chirodactyl\":3}}}," +
            "\"UnknownField\":{\"value\":42}}");

        void AssertMigrated(Player migrated)
        {
            migrated.Quests["cap_primal_adept"].KillProgresses.Should().Equal(new Dictionary<NPCGroupType, int> { [NPCGroupType.Dathomir_YoungGrottoBeasts] = 4 });
            migrated.Quests["cap_untouch_adept"].KillProgresses.Should().Equal(new Dictionary<NPCGroupType, int> { [NPCGroupType.Dathomir_YoungGrottoBeasts] = 2 });
            migrated.Quests["cap_alpha_adept"].KillProgresses.Should().Equal(new Dictionary<NPCGroupType, int> { [NPCGroupType.Dathomir_YoungGrottoBeasts] = 1 },
                "an entry already under the new name is kept");
            migrated.Quests["other_quest"].KillProgresses.Should().Equal(new Dictionary<NPCGroupType, int> { [NPCGroupType.Dathomir_Chirodactyl] = 3 });
        }

        // A record that skipped server migration 30 (e.g. restored afterwards) still loads, with the former names mapped.
        AssertMigrated(JsonConvert.DeserializeObject<Player>(player.ToString())!);

        // Server migration 30 rewrites the raw record, which then saves and reloads under the new names.
        var raw = (JObject)player.DeepClone();
        _30_ConsolidatePelvisAppearance.RenameCapstoneNpcGroups(raw, out var rewritten).Should().BeTrue();
        AssertMigrated(rewritten);
        raw[nameof(Player.Quests)]!["cap_untouch_adept"]![nameof(PlayerQuest.KillProgresses)]!
            .Value<JObject>()!.Properties().Select(entry => entry.Name).Should().Equal(nameof(NPCGroupType.Dathomir_YoungGrottoBeasts));
        _30_ConsolidatePelvisAppearance.RenameCapstoneNpcGroups(raw, out _).Should().BeFalse("the rewrite runs once");

        var saved = JsonConvert.SerializeObject(rewritten);
        for (var round = 0; round < 2; round++)
        {
            var reloaded = JsonConvert.DeserializeObject<Player>(saved)!;
            AssertMigrated(reloaded);
            saved = JsonConvert.SerializeObject(reloaded);
        }
    }
}
