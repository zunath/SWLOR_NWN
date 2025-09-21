using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IQuestService
    {
        void CacheData();
        void RegisterQuests();
        void LoadPlayerQuests();
        QuestDetail GetQuestById(string questId);
        List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType npcGroupType);
        void AbandonQuest(uint player, string questId);
        void AcceptQuest(uint player, string questId);
        void AdvanceQuest(uint player, uint questSource, string questId);
        void RequestItemsFromPlayer(uint player, string questId);
        void ProgressKillTargetObjectives();
        void OpenItemCollector();
        void CloseItemCollector();
        void DisturbItemCollector();
        void UseQuestPlaceable();
        void EnterQuestTrigger();
        void TriggerAndPlaceableProgression(uint player, uint triggerOrPlaceable);
        int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount);
        List<QuestDetail> GetQuestsByGuild(GuildType guild, int rank);
    }
}
