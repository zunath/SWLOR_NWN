using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Quest.Model
{
    public class KillTargetObjective : IQuestObjective
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();
        private INPCGroupService NPCGroupService => _serviceProvider.GetRequiredService<INPCGroupService>();
        public NPCGroupType Group { get; }
        private readonly int _amount;

        public KillTargetObjective(IDatabaseService db, IServiceProvider serviceProvider, NPCGroupType group, int amount)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            Group = group;
            _amount = amount;
        }

        public void Initialize(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : new PlayerQuest();
            
            quest.KillProgresses[Group] = _amount;
            dbPlayer.Quests[questId] = quest;
            _db.Set(dbPlayer);
        }

        public void Advance(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;

            if (quest == null) return;
            if (!quest.KillProgresses.ContainsKey(Group)) return;
            if (quest.KillProgresses[Group] <= 0) return;

            quest.KillProgresses[Group]--;
            _db.Set(dbPlayer);

            var npcGroup = NPCGroupService.GetNPCGroup(Group);
            var questDetail = QuestService.GetQuestById(questId);

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
            var dbPlayer = _db.Get<Player>(playerId);
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
            var dbPlayer = _db.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId))
                return "N/A";

            var npcGroup = NPCGroupService.GetNPCGroup(Group);
            var numberRemaining = dbPlayer.Quests[questId].KillProgresses[Group];
            
            return $"{_amount - numberRemaining} / {_amount} {npcGroup.Name}";
        }
    }
}
