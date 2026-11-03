using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>Credits Dorra Vesk's stablemaster trials when a player extracts DNA from a high-level beast.</summary>
    public static class BeastStableTrials
    {
        public const int TrialOneMinimumDnaLevel = 40;
        public const int TrialTwoMinimumDnaLevel = 45;

        public static void RecordDnaExtraction(uint player, int beastLevel, uint source)
        {
            foreach (var objective in GetDnaObjectiveKeys(beastLevel))
                Quest.RecordCombatObjective(player, objective, source);
        }

        public static IReadOnlyList<string> GetDnaObjectiveKeys(int beastLevel)
        {
            var keys = new List<string>();
            if (beastLevel >= TrialOneMinimumDnaLevel)
                keys.Add(BeastStableQuestDefinition.DnaLevel40Objective);
            if (beastLevel >= TrialTwoMinimumDnaLevel)
                keys.Add(BeastStableQuestDefinition.DnaLevel45Objective);

            return keys;
        }
    }
}
