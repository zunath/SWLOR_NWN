using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Renames the persisted enum names of dungeons that moved to another planet. Player key items and quest kill
/// counters are dictionaries keyed by enum name, so stale names must be rewritten before a Player deserializes.
/// </summary>
public static class RelocatedDungeonEnumMigration
{
    public static readonly IReadOnlyDictionary<string, NPCGroupType> NPCGroupRenames = new Dictionary<string, NPCGroupType>
    {
        ["Korriban_AbsoluteDefense_Adept"] = NPCGroupType.Tatooine_AbsoluteDefense_Adept,
        ["Korriban_AbsoluteDefense_Specialist"] = NPCGroupType.Tatooine_AbsoluteDefense_Specialist,
        ["Korriban_AbsoluteDefense_Warden"] = NPCGroupType.Tatooine_AbsoluteDefense_Warden,
        ["Korriban_AbsoluteDefense_InnerCircle"] = NPCGroupType.Tatooine_AbsoluteDefense_InnerCircle,
        ["Korriban_AbsoluteDefense_Master"] = NPCGroupType.Tatooine_AbsoluteDefense_Master,
        ["Korriban_SoulAscension_Adept"] = NPCGroupType.Tatooine_SoulAscension_Adept,
        ["Korriban_SoulAscension_Specialist"] = NPCGroupType.Tatooine_SoulAscension_Specialist,
        ["Korriban_SoulAscension_Warden"] = NPCGroupType.Tatooine_SoulAscension_Warden,
        ["Korriban_SoulAscension_InnerCircle"] = NPCGroupType.Tatooine_SoulAscension_InnerCircle,
        ["Korriban_SoulAscension_Master"] = NPCGroupType.Tatooine_SoulAscension_Master,
        ["Korriban_Forcebane_Adept"] = NPCGroupType.Tatooine_Forcebane_Adept,
        ["Korriban_Forcebane_Specialist"] = NPCGroupType.Tatooine_Forcebane_Specialist,
        ["Korriban_Forcebane_Warden"] = NPCGroupType.Tatooine_Forcebane_Warden,
        ["Korriban_Forcebane_InnerCircle"] = NPCGroupType.Tatooine_Forcebane_InnerCircle,
        ["Korriban_Forcebane_Master"] = NPCGroupType.Tatooine_Forcebane_Master,
        ["Dantooine_HoldTheLine_Adept"] = NPCGroupType.MonCala_HoldTheLine_Adept,
        ["Dantooine_HoldTheLine_Specialist"] = NPCGroupType.MonCala_HoldTheLine_Specialist,
        ["Dantooine_HoldTheLine_Warden"] = NPCGroupType.MonCala_HoldTheLine_Warden,
        ["Dantooine_HoldTheLine_InnerCircle"] = NPCGroupType.MonCala_HoldTheLine_InnerCircle,
        ["Dantooine_HoldTheLine_Master"] = NPCGroupType.MonCala_HoldTheLine_Master,
        ["Dantooine_EmergencyCocktail_Adept"] = NPCGroupType.MonCala_EmergencyCocktail_Adept,
        ["Dantooine_EmergencyCocktail_Specialist"] = NPCGroupType.MonCala_EmergencyCocktail_Specialist,
        ["Dantooine_EmergencyCocktail_Warden"] = NPCGroupType.MonCala_EmergencyCocktail_Warden,
        ["Dantooine_EmergencyCocktail_InnerCircle"] = NPCGroupType.MonCala_EmergencyCocktail_InnerCircle,
        ["Dantooine_EmergencyCocktail_Master"] = NPCGroupType.MonCala_EmergencyCocktail_Master,
        ["Dantooine_InfiniteConduit_Adept"] = NPCGroupType.MonCala_InfiniteConduit_Adept,
        ["Dantooine_InfiniteConduit_Specialist"] = NPCGroupType.MonCala_InfiniteConduit_Specialist,
        ["Dantooine_InfiniteConduit_Warden"] = NPCGroupType.MonCala_InfiniteConduit_Warden,
        ["Dantooine_InfiniteConduit_InnerCircle"] = NPCGroupType.MonCala_InfiniteConduit_InnerCircle,
        ["Dantooine_InfiniteConduit_Master"] = NPCGroupType.MonCala_InfiniteConduit_Master,
        ["Dantooine_StormDrillDroid"] = NPCGroupType.Hutlar_StormDrillDroid,
        ["Dantooine_StormDuelistDroid"] = NPCGroupType.Hutlar_StormDuelistDroid,
        ["Dantooine_StormGatekeeperDroid"] = NPCGroupType.Hutlar_StormGatekeeperDroid,
        ["Dantooine_StormExaminerDroid"] = NPCGroupType.Hutlar_StormExaminerDroid,
        ["Dantooine_TempestTrainingEngine"] = NPCGroupType.Hutlar_TempestTrainingEngine,
        ["Dantooine_GuardianPatrolDroid"] = NPCGroupType.Hutlar_GuardianPatrolDroid,
        ["Dantooine_GuardianBulwarkDroid"] = NPCGroupType.Hutlar_GuardianBulwarkDroid,
        ["Dantooine_GuardianGatekeeperDroid"] = NPCGroupType.Hutlar_GuardianGatekeeperDroid,
        ["Dantooine_GuardianShieldDroid"] = NPCGroupType.Hutlar_GuardianShieldDroid,
        ["Dantooine_BastionTrainingEngine"] = NPCGroupType.Hutlar_BastionTrainingEngine,
        ["Dantooine_CycloneDrillDroid"] = NPCGroupType.Hutlar_CycloneDrillDroid,
        ["Dantooine_CycloneDuelistDroid"] = NPCGroupType.Hutlar_CycloneDuelistDroid,
        ["Dantooine_CycloneGatekeeperDroid"] = NPCGroupType.Hutlar_CycloneGatekeeperDroid,
        ["Dantooine_CycloneExaminerDroid"] = NPCGroupType.Hutlar_CycloneExaminerDroid,
        ["Dantooine_VortexTrainingEngine"] = NPCGroupType.Hutlar_VortexTrainingEngine,
        ["Korriban_CryptRaider"] = NPCGroupType.MonCala_CryptRaider,
        ["Korriban_CryptMarauder"] = NPCGroupType.MonCala_CryptMarauder,
        ["Korriban_CryptBreachCaptain"] = NPCGroupType.MonCala_CryptBreachCaptain,
        ["Korriban_CryptReaver"] = NPCGroupType.MonCala_CryptReaver,
        ["Korriban_CryptExpeditionLeader"] = NPCGroupType.MonCala_CryptExpeditionLeader,
        ["Korriban_VitaeReaverInitiate"] = NPCGroupType.MonCala_VitaeReaverInitiate,
        ["Korriban_VitaeReaverAdept"] = NPCGroupType.MonCala_VitaeReaverAdept,
        ["Korriban_VitaeReaverEnforcer"] = NPCGroupType.MonCala_VitaeReaverEnforcer,
        ["Korriban_VitaeReaverBinder"] = NPCGroupType.MonCala_VitaeReaverBinder,
        ["Korriban_VitaeReaverChief"] = NPCGroupType.MonCala_VitaeReaverChief,
        ["Korriban_UmbralRaiderScout"] = NPCGroupType.MonCala_UmbralRaiderScout,
        ["Korriban_UmbralRaiderAdept"] = NPCGroupType.MonCala_UmbralRaiderAdept,
        ["Korriban_UmbralRaiderCaptain"] = NPCGroupType.MonCala_UmbralRaiderCaptain,
        ["Korriban_UmbralRaiderSaboteur"] = NPCGroupType.MonCala_UmbralRaiderSaboteur,
        ["Korriban_UmbralRaiderCommander"] = NPCGroupType.MonCala_UmbralRaiderCommander,
    };

    public static readonly IReadOnlyDictionary<string, KeyItemType> KeyItemRenames = new Dictionary<string, KeyItemType>
    {
        ["CapstoneDantooineJediEnclaveTrialHallsKey"] = KeyItemType.CapstoneHutlarJediTrialHallsKey,
        ["CapstoneKorribanForgeCavernsKey"] = KeyItemType.CapstoneTatooineForgeCavernsKey,
        ["CapstoneKorribanSithCryptDepthsKey"] = KeyItemType.CapstoneMonCalaSithCryptDepthsKey,
        ["CapstoneDantooineMedicalSublevelKey"] = KeyItemType.CapstoneMonCalaMedicalSublevelKey,
    };

    public static bool RenameKeyItems(JObject keyItems) =>
        RenameKeys(keyItems, KeyItemRenames, (existing, _) => existing);

    // Counters store kills remaining: the smaller value retains the most progress.
    public static bool RenameKillProgresses(JObject killProgresses) =>
        RenameKeys(killProgresses, NPCGroupRenames,
            (existing, renamed) => new JValue(Math.Min(existing.Value<int>(), renamed.Value<int>())));

    private static bool RenameKeys<T>(JObject entries, IReadOnlyDictionary<string, T> renames, Func<JToken, JToken, JToken> merge)
        where T : struct, Enum
    {
        if (entries == null) return false;
        var changed = false;
        foreach (var entry in entries.Properties().ToArray())
        {
            if (!renames.TryGetValue(entry.Name, out var replacement)) continue;
            var name = replacement.ToString();
            var existing = entries.Property(name) ?? entries.Property(Convert.ToInt32(replacement).ToString());
            if (existing == null)
                entries[name] = entry.Value.DeepClone();
            else
                existing.Value = merge(existing.Value, entry.Value);
            entry.Remove();
            changed = true;
        }
        return changed;
    }
}
