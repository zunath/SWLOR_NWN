using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.QuestRule.Contracts
{
    public interface IQuestRule
    {
        void Run(NWPlayer player, NWObject questSource, int questID, string[] args);
    }
}
