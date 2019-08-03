using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestObjective
    {
        bool IsComplete(NWPlayer player);
    }
}
