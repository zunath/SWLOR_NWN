using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IQuestService
    {
        bool CanAcceptQuest(NWPlayer oPC, int questID, bool sendMessage);
        bool CanAcceptQuest(NWPlayer oPC, Quest quest, bool sendMessage);
        void AcceptQuest(NWPlayer player, NWObject questOwner, int questID);
        void AdvanceQuestState(NWPlayer player, NWObject questOwner, int questID);
        void CompleteQuest(NWPlayer player, NWObject questOwner, int questID, ItemVO selectedItem);
        int GetPlayerQuestJournalID(NWObject oPC, int questID);
        Quest GetQuestByID(int questID);
        ItemVO GetTempItemInformation(string resref, int quantity);
        bool HasPlayerCompletedQuest(NWObject oPC, int questID);
        void OnClientEnter();
        void OnCreatureDeath(NWCreature creature);
        void OnModuleItemAcquired();
        void OnQuestPlaceableUsed(NWObject oObject);
        void OnQuestTriggerEntered(NWObject oObject);
        void RequestItemsFromPC(NWPlayer oPC, NWObject questOwner, int questID);
        void SpawnQuestItems(NWPlaceable oChest, NWPlayer oPC);
    }
}
