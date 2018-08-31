using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IQuestService
    {
        void AcceptQuest(NWPlayer oPC, int questID);
        void AdvanceQuestState(NWPlayer oPC, int questID);
        void CompleteQuest(NWPlayer player, int questID, ItemVO selectedItem);
        int GetPlayerQuestJournalID(NWObject oPC, int questID);
        Quest GetQuestByID(int questID);
        ItemVO GetTempItemInformation(string resref, int quantity);
        bool HasPlayerCompletedQuest(NWObject oPC, int questID);
        void OnClientEnter();
        void OnCreatureDeath(NWCreature creature);
        void OnItemCollectorClosed(NWObject container);
        void OnItemCollectorDisturbed(NWPlaceable container);
        void OnItemCollectorOpened(NWPlaceable container);
        void OnModuleItemAcquired();
        void OnQuestPlaceableUsed(NWObject oObject);
        void OnQuestTriggerEntered(NWObject oObject);
        void RequestItemsFromPC(NWPlayer oPC, NWObject questOwner, int questID, int sequenceID);
        void SpawnQuestItems(NWPlaceable oChest, NWPlayer oPC);
    }
}
