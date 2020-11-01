using System.Collections.Generic;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Quest.Contracts
{
    public interface IQuestState
    {
        void AddObjective(IQuestObjective objective);
        IEnumerable<IQuestObjective> GetObjectives();
        bool IsComplete(NWPlayer player, int questID);
    }
}
