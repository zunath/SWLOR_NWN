using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestObjective
    {
        void Initialize(uint player, string questId);
        void Advance(uint player, string questId);
        bool IsComplete(uint player, string questId);
        string GetCurrentStateText(uint player, string questId);
    }

    public class CollectItemObjective : IQuestObjective
    {
        private readonly string _resref;
        private readonly int _quantity;

        public CollectItemObjective(string resref, int quantity)
        {
            _resref = resref;
            _quantity = quantity;
        }

        public void Initialize(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : new PlayerQuest();

            quest.ItemProgresses[_resref] = _quantity;
            dbPlayer.Quests[questId] = quest;
            DB.Set(dbPlayer);
        }

        public void Advance(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return;
            if (!quest.ItemProgresses.ContainsKey(_resref)) return;
            if (quest.ItemProgresses[_resref] <= 0) return;

            quest.ItemProgresses[_resref]--;
            DB.Set(dbPlayer);

            var questDetail = Quest.GetQuestById(questId);
            var itemName = Cache.GetItemNameByResref(_resref);

            var statusMessage = $"[{questDetail.Name}] {itemName} remaining: {quest.ItemProgresses[_resref]}";

            if (quest.ItemProgresses[_resref] <= 0)
            {
                statusMessage += $" {ColorToken.Green("{COMPLETE}")}";
            }

            SendMessageToPC(player, statusMessage);
        }

        public bool IsComplete(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return false;

            foreach (var progress in quest.ItemProgresses.Values)
            {
                if (progress > 0)
                    return false;
            }

            return true;
        }

        public string GetCurrentStateText(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId))
                return "N/A";
            if (!dbPlayer.Quests[questId].ItemProgresses.ContainsKey(_resref))
                return "N/A";

            var numberRemaining = dbPlayer.Quests[questId].ItemProgresses[_resref];
            var itemName = Cache.GetItemNameByResref(_resref);
            return $"{_quantity - numberRemaining} / {_quantity} {itemName}";
        }
    }

    public class KillTargetObjective : IQuestObjective
    {
        public NPCGroupType Group { get; }
        private readonly int _amount;

        public KillTargetObjective(NPCGroupType group, int amount)
        {
            Group = group;
            _amount = amount;
        }

        public void Initialize(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : new PlayerQuest();
            
            quest.KillProgresses[Group] = _amount;
            dbPlayer.Quests[questId] = quest;
            DB.Set(dbPlayer);
        }

        public void Advance(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return;
            if (!quest.KillProgresses.ContainsKey(Group)) return;
            if (quest.KillProgresses[Group] <= 0) return;

            quest.KillProgresses[Group]--;
            DB.Set(dbPlayer);

            var npcGroup = NPCGroup.GetNPCGroup(Group);
            var questDetail = Quest.GetQuestById(questId);

            var statusMessage = $"[{questDetail.Name}] {npcGroup.Name} remaining: {quest.KillProgresses[Group]}";

            if (quest.KillProgresses[Group] <= 0)
            {
                statusMessage += $" {ColorToken.Green("{COMPLETE}")}";
            }

            SendMessageToPC(player, statusMessage);
        }

        public bool IsComplete(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return false;

            foreach (var progress in quest.KillProgresses.Values)
            {
                if (progress > 0)
                    return false;
            }

            return true;
        }

        public string GetCurrentStateText(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId))
                return "N/A";

            var npcGroup = NPCGroup.GetNPCGroup(Group);
            var numberRemaining = dbPlayer.Quests[questId].KillProgresses[Group];
            
            return $"{_amount - numberRemaining} / {_amount} {npcGroup.Name}";
        }
    }
}
