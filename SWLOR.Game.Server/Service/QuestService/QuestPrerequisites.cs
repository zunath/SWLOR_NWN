using SWLOR.Game.Server.Core.NWScript;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(uint player);
    }

    public class RequiredQuestPrerequisite : IQuestPrerequisite
    {
        private readonly string _questID;

        public RequiredQuestPrerequisite(string questID)
        {
            _questID = questID;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = NWScript.GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var timesCompleted = dbPlayer.Quests.ContainsKey(_questID) ? dbPlayer.Quests[_questID].TimesCompleted : 0;
            return timesCompleted > 0;
        }
    }
}
