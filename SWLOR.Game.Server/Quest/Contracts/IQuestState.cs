using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestState
    {
        void AddObjective(IQuestObjective objective);
        IEnumerable<IQuestObjective> GetObjectives();
        bool IsComplete(NWPlayer player, int questID);
    }
}
