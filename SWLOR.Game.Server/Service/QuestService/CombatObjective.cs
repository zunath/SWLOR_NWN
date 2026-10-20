using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>A counter credited by a completed gameplay event, never by a dialogue action.</summary>
    public sealed class CombatObjective : IQuestObjective
    {
        public string Key { get; }
        public string Description { get; }
        public int Amount { get; }

        public CombatObjective(string key, string description, int amount)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("An objective key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("An objective description is required.", nameof(description));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Key = key;
            Description = description;
            Amount = amount;
        }

        public void Initialize(PlayerQuest progress) => progress.CombatProgresses[Key] = Amount;

        public bool TryAdvance(PlayerQuest progress)
        {
            if (!progress.CombatProgresses.TryGetValue(Key, out var remaining) || remaining <= 0)
                return false;
            progress.CombatProgresses[Key] = remaining - 1;
            return true;
        }

        public bool IsComplete(PlayerQuest progress) =>
            progress.CombatProgresses.TryGetValue(Key, out var remaining) && remaining <= 0;

        public void Initialize(uint player, string questId)
        {
            var record = DB.Get<Player>(GetObjectUUID(player));
            if (!record.Quests.TryGetValue(questId, out var progress))
                record.Quests[questId] = progress = new PlayerQuest();
            Initialize(progress);
            DB.Set(record);
        }

        public void Advance(uint player, string questId)
        {
            var record = DB.Get<Player>(GetObjectUUID(player));
            if (!record.Quests.TryGetValue(questId, out var progress) || !TryAdvance(progress))
                return;
            DB.Set(record);
            SendMessageToPC(player, $"[{Quest.GetQuestById(questId).Name}] {Description}: {Amount - progress.CombatProgresses[Key]} / {Amount}");
        }

        public bool IsComplete(uint player, string questId)
        {
            var record = DB.Get<Player>(GetObjectUUID(player));
            return record.Quests.TryGetValue(questId, out var progress) && IsComplete(progress);
        }

        public string GetCurrentStateText(uint player, string questId)
        {
            var record = DB.Get<Player>(GetObjectUUID(player));
            return record.Quests.TryGetValue(questId, out var progress) && progress.CombatProgresses.TryGetValue(Key, out var remaining)
                ? $"{Amount - remaining} / {Amount} {Description}"
                : "N/A";
        }
    }
}
