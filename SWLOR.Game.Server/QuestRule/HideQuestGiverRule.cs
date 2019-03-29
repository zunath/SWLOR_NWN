using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.QuestRule
{
    public class HideQuestGiverRule: IQuestRule
    {
        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            ObjectVisibilityService.AdjustVisibility(player, questSource, false);
        }
    }
}
