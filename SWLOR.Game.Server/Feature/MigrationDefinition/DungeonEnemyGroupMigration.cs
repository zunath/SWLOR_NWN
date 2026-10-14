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
        [NPCGroupType.Dantooine_SaberStorm_Adept] = NPCGroupType.Hutlar_StormDrillDroid,
        [NPCGroupType.Dantooine_SaberStorm_Specialist] = NPCGroupType.Hutlar_StormDuelistDroid,
        [NPCGroupType.Dantooine_SaberStorm_Warden] = NPCGroupType.Hutlar_StormGatekeeperDroid,
        [NPCGroupType.Dantooine_SaberStorm_InnerCircle] = NPCGroupType.Hutlar_StormExaminerDroid,
        [NPCGroupType.Dantooine_SaberStorm_Master] = NPCGroupType.Hutlar_TempestTrainingEngine,
        [NPCGroupType.Dantooine_GuardianMaster_Adept] = NPCGroupType.Hutlar_GuardianPatrolDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Specialist] = NPCGroupType.Hutlar_GuardianBulwarkDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Warden] = NPCGroupType.Hutlar_GuardianGatekeeperDroid,
        [NPCGroupType.Dantooine_GuardianMaster_InnerCircle] = NPCGroupType.Hutlar_GuardianShieldDroid,
        [NPCGroupType.Dantooine_GuardianMaster_Paragon] = NPCGroupType.Hutlar_BastionTrainingEngine,
        [NPCGroupType.Dantooine_SaberCyclone_Adept] = NPCGroupType.Hutlar_CycloneDrillDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Specialist] = NPCGroupType.Hutlar_CycloneDuelistDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Warden] = NPCGroupType.Hutlar_CycloneGatekeeperDroid,
        [NPCGroupType.Dantooine_SaberCyclone_InnerCircle] = NPCGroupType.Hutlar_CycloneExaminerDroid,
        [NPCGroupType.Dantooine_SaberCyclone_Master] = NPCGroupType.Hutlar_VortexTrainingEngine,
        [NPCGroupType.Korriban_LastStandOfTheLight_Adept] = NPCGroupType.MonCala_CryptRaider,
        [NPCGroupType.Korriban_LastStandOfTheLight_Specialist] = NPCGroupType.MonCala_CryptMarauder,
        [NPCGroupType.Korriban_LastStandOfTheLight_Warden] = NPCGroupType.MonCala_CryptBreachCaptain,
        [NPCGroupType.Korriban_LastStandOfTheLight_InnerCircle] = NPCGroupType.MonCala_CryptReaver,
        [NPCGroupType.Korriban_LastStandOfTheLight_Master] = NPCGroupType.MonCala_CryptExpeditionLeader,
        [NPCGroupType.Korriban_HungerOfTheDark_Adept] = NPCGroupType.MonCala_VitaeReaverInitiate,
        [NPCGroupType.Korriban_HungerOfTheDark_Specialist] = NPCGroupType.MonCala_VitaeReaverAdept,
        [NPCGroupType.Korriban_HungerOfTheDark_Warden] = NPCGroupType.MonCala_VitaeReaverEnforcer,
        [NPCGroupType.Korriban_HungerOfTheDark_InnerCircle] = NPCGroupType.MonCala_VitaeReaverBinder,
        [NPCGroupType.Korriban_HungerOfTheDark_Master] = NPCGroupType.MonCala_VitaeReaverChief,
        [NPCGroupType.Korriban_EclipseOfResolve_Adept] = NPCGroupType.MonCala_UmbralRaiderScout,
        [NPCGroupType.Korriban_EclipseOfResolve_Specialist] = NPCGroupType.MonCala_UmbralRaiderAdept,
        [NPCGroupType.Korriban_EclipseOfResolve_Warden] = NPCGroupType.MonCala_UmbralRaiderCaptain,
        [NPCGroupType.Korriban_EclipseOfResolve_InnerCircle] = NPCGroupType.MonCala_UmbralRaiderSaboteur,
        [NPCGroupType.Korriban_EclipseOfResolve_Master] = NPCGroupType.MonCala_UmbralRaiderCommander,
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
