using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Quest.Contracts
{
    public interface IQuestObjective
    {
        void Initialize(NWPlayer player, int questID);
        bool IsComplete(NWPlayer player, int questID);
    }
}
