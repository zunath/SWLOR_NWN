using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestObjective
    {
        void Initialize(NWPlayer player, int questID);
        bool IsComplete(NWPlayer player, int questID);
    }
}
