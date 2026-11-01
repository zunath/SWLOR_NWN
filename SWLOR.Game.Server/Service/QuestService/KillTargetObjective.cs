using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service.QuestService
{
    public class KillTargetObjective : IQuestObjective
    {
        public NPCGroupType Group { get; }
        public int Amount { get; }
        public float MaximumCreditDistance { get; }

        public KillTargetObjective(NPCGroupType group, int amount, float maximumCreditDistance = 50f)
        {
            if (!float.IsFinite(maximumCreditDistance) || maximumCreditDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumCreditDistance));

            MaximumCreditDistance = maximumCreditDistance;
            Group = group;
            Amount = amount;
        }

        /// <summary>Whether this kill of the objective's group counts for this party member.</summary>
        public virtual bool CountsKill(uint player, uint creature) => true;

        public void Initialize(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var quest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : new PlayerQuest();

            quest.KillProgresses[Group] = Amount;
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

            return quest.KillProgresses.TryGetValue(Group, out var remaining) && remaining <= 0;
        }

        public string GetCurrentStateText(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId))
                return "N/A";

            var npcGroup = NPCGroup.GetNPCGroup(Group);
            var numberRemaining = dbPlayer.Quests[questId].KillProgresses[Group];

            return $"{Amount - numberRemaining} / {Amount} {npcGroup.Name}";
        }
    }
}
