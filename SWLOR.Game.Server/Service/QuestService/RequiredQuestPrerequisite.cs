using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
{
    public class RequiredQuestPrerequisite : IQuestPrerequisite
    {
        private readonly IDatabaseService _db;
        private readonly string _questID;

        public RequiredQuestPrerequisite(IDatabaseService db, string questID)
        {
            _db = db;
            _questID = questID;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var timesCompleted = dbPlayer.Quests.ContainsKey(_questID) ? dbPlayer.Quests[_questID].TimesCompleted : 0;
            return timesCompleted > 0;
        }
    }
}
