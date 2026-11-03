using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class EshanDailySupplyPrerequisite : IQuestPrerequisite
    {
        private readonly string _questId;
        public static TimeSpan RepeatDelay { get; } = TimeSpan.FromHours(24);

        public EshanDailySupplyPrerequisite(string questId)
        {
            _questId = questId;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            dbPlayer.Quests.TryGetValue(_questId, out var quest);
            return IsEligible(quest, DateTime.UtcNow);
        }

        public static bool IsEligible(PlayerQuest quest, DateTime utcNow)
        {
            if (quest == null)
                return true;

            // Accepting clears the completion date. A null date means the current supply
            // order is active, even when the player has completed earlier orders.
            if (!quest.DateLastCompleted.HasValue)
                return false;

            // The quest lifecycle also stamps this date when a repeated order is abandoned.
            // Reusing the persisted date keeps cancellation and server restarts from opening
            // another order early.
            return utcNow - quest.DateLastCompleted.Value >= RepeatDelay;
        }
    }
}
