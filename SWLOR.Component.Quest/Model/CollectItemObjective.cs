using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.UI.Service;
using Player = SWLOR.Shared.Core.Data.Entity.Player;

namespace SWLOR.Component.Quest.Model
{
    public class CollectItemObjective : IQuestObjective
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly IQuestService _questService;

        private readonly string _resref;
        private readonly int _quantity;

        public CollectItemObjective(IDatabaseService db, IItemCacheService itemCache, IQuestService questService, string resref, int quantity)
        {
            _db = db;
            _itemCache = itemCache;
            _questService = questService;
            _resref = resref;
            _quantity = quantity;
        }

        public void Initialize(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : new PlayerQuest();

            quest.ItemProgresses[_resref] = _quantity;
            dbPlayer.Quests[questId] = quest;
            _db.Set(dbPlayer);
        }

        public void Advance(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return;
            if (!quest.ItemProgresses.ContainsKey(_resref)) return;
            if (quest.ItemProgresses[_resref] <= 0) return;

            quest.ItemProgresses[_resref]--;
            _db.Set(dbPlayer);

            var questDetail = _questService.GetQuestById(questId);
            var itemName = _itemCache.GetItemNameByResref(_resref);

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
            var dbPlayer = _db.Get<Player>(playerId);
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
            var dbPlayer = _db.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId))
                return "N/A";
            if (!dbPlayer.Quests[questId].ItemProgresses.ContainsKey(_resref))
                return "N/A";

            var numberRemaining = dbPlayer.Quests[questId].ItemProgresses[_resref];
            var itemName = _itemCache.GetItemNameByResref(_resref);
            return $"{_quantity - numberRemaining} / {_quantity} {itemName}";
        }
    }
}
