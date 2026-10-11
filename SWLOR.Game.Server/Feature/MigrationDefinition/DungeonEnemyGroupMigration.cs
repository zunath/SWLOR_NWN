using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

public static class DungeonEnemyGroupMigration
{
    private static readonly Dictionary<NPCGroupType, NPCGroupType> Replacements = new()
    {
        [NPCGroupType.Dantooine_SaberStorm_Adept] = NPCGroupType.Dantooine_StormDrillDroid,
        [NPCGroupType.Dantooine_SaberStorm_Specialist] = NPCGroupType.Dantooine_StormDuelistDroid,
        [NPCGroupType.Dantooine_SaberStorm_Warden] = NPCGroupType.Dantooine_StormGatekeeperDroid,
        [NPCGroupType.Dantooine_SaberStorm_InnerCircle] = NPCGroupType.Dantooine_StormExaminerDroid,
        [NPCGroupType.Dantooine_SaberStorm_Master] = NPCGroupType.Dantooine_TempestTrainingEngine,
        [NPCGroupType.Dantooine_GuardianMaster_Adept] = NPCGroupType.Dantooine_GuardianPatrolDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Specialist] = NPCGroupType.Dantooine_GuardianBulwarkDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Warden] = NPCGroupType.Dantooine_GuardianGatekeeperDroid,
        [NPCGroupType.Dantooine_GuardianMaster_InnerCircle] = NPCGroupType.Dantooine_GuardianShieldDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Paragon] = NPCGroupType.Dantooine_BastionTrainingEngine,
        [NPCGroupType.Dantooine_SaberCyclone_Adept] = NPCGroupType.Dantooine_CycloneDrillDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Specialist] = NPCGroupType.Dantooine_CycloneDuelistDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Warden] = NPCGroupType.Dantooine_CycloneGatekeeperDroid,
        [NPCGroupType.Dantooine_SaberCyclone_InnerCircle] = NPCGroupType.Dantooine_CycloneExaminerDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Master] = NPCGroupType.Dantooine_VortexTrainingEngine,
        [NPCGroupType.Korriban_LastStandOfTheLight_Adept] = NPCGroupType.Korriban_CryptRaider,
        [NPCGroupType.Korriban_LastStandOfTheLight_Specialist] = NPCGroupType.Korriban_CryptMarauder,
        [NPCGroupType.Korriban_LastStandOfTheLight_Warden] = NPCGroupType.Korriban_CryptBreachCaptain,
        [NPCGroupType.Korriban_LastStandOfTheLight_InnerCircle] = NPCGroupType.Korriban_CryptReaver,
        [NPCGroupType.Korriban_LastStandOfTheLight_Master] = NPCGroupType.Korriban_CryptExpeditionLeader,
        [NPCGroupType.Korriban_HungerOfTheDark_Adept] = NPCGroupType.Korriban_VitaeReaverInitiate,
        [NPCGroupType.Korriban_HungerOfTheDark_Specialist] = NPCGroupType.Korriban_VitaeReaverAdept,
        [NPCGroupType.Korriban_HungerOfTheDark_Warden] = NPCGroupType.Korriban_VitaeReaverEnforcer,
        [NPCGroupType.Korriban_HungerOfTheDark_InnerCircle] = NPCGroupType.Korriban_VitaeReaverBinder,
        [NPCGroupType.Korriban_HungerOfTheDark_Master] = NPCGroupType.Korriban_VitaeReaverChief,
        [NPCGroupType.Korriban_EclipseOfResolve_Adept] = NPCGroupType.Korriban_UmbralRaiderScout,
        [NPCGroupType.Korriban_EclipseOfResolve_Specialist] = NPCGroupType.Korriban_UmbralRaiderAdept,
        [NPCGroupType.Korriban_EclipseOfResolve_Warden] = NPCGroupType.Korriban_UmbralRaiderCaptain,
        [NPCGroupType.Korriban_EclipseOfResolve_InnerCircle] = NPCGroupType.Korriban_UmbralRaiderSaboteur,
        [NPCGroupType.Korriban_EclipseOfResolve_Master] = NPCGroupType.Korriban_UmbralRaiderCommander,
    };

    public static NPCGroupType GetReplacement(NPCGroupType group) => Replacements.GetValueOrDefault(group, group);

    public static bool MigrateQuestProgress(Player player)
    {
        var changed = false;
        if (player.Quests == null) return false;
        foreach (var quest in player.Quests.Values)
        {
            if (quest?.KillProgresses == null) continue;
            foreach (var (group, remaining) in quest.KillProgresses.ToArray())
            {
                var replacement = GetReplacement(group);
                if (replacement == group) continue;
                // Counters store kills remaining: the smaller value retains the most progress.
                quest.KillProgresses[replacement] = quest.KillProgresses.TryGetValue(replacement, out var current)
                    ? Math.Min(current, remaining) : remaining;
                quest.KillProgresses.Remove(group);
                changed = true;
            }
        }
        return changed;
    }

    public static bool MigrateCreature(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original) || !StoredObjectData.IsCreatureData(original)) return false;
        migrated = StoredObjectData.RewriteRootInteger(original, "QUEST_NPC_GROUP_ID",
            value => (int)GetReplacement((NPCGroupType)value));
        return migrated != original;
    }
}
