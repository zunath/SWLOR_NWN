using System;
using System.Linq;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.PerkService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// How many beasts a player may own: one, plus one per Stabling perk level, plus one per completed part of
    /// Dorra Vesk's stablemaster quest line. The quest bonus is read from completed quest records, so it needs no
    /// stored field of its own.
    /// </summary>
    public static class BeastStableCapacity
    {
        public const int BaseCapacity = 1;

        /// <summary>The number of beasts this player may own, counting the active beast.</summary>
        public static int GetCapacity(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return Calculate(Perk.GetPerkLevel(player, PerkType.Stabling), CountCompletedStableQuests(dbPlayer));
        }

        public static int Calculate(int stablingPerkLevel, int completedStableQuests)
        {
            return BaseCapacity +
                   Math.Max(0, stablingPerkLevel) +
                   Math.Clamp(completedStableQuests, 0, BeastStableQuestDefinition.StableSlotQuestIds.Count);
        }

        /// <summary>How many parts of the stablemaster quest line this player has completed.</summary>
        public static int CountCompletedStableQuests(Player dbPlayer)
        {
            if (dbPlayer?.Quests == null)
                return 0;

            return BeastStableQuestDefinition.StableSlotQuestIds.Count(questId =>
                dbPlayer.Quests.TryGetValue(questId, out var quest) && quest.TimesCompleted > 0);
        }

        /// <summary>Whether this quest's completion changes stable capacity.</summary>
        public static bool IsStableSlotQuest(string questId) =>
            BeastStableQuestDefinition.StableSlotQuestIds.Contains(questId);
    }
}
