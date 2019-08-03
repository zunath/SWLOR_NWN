using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestObjective
    {
        void Initialize(NWPlayer player, PCQuestStatus status);
        bool IsComplete(NWPlayer player);
    }
}
