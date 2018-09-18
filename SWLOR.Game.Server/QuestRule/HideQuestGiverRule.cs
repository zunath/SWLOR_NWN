using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class HideQuestGiverRule: IQuestRule
    {
        private readonly IObjectVisibilityService _visibility;

        public HideQuestGiverRule(IObjectVisibilityService visibility)
        {
            _visibility = visibility;
        }

        public void Run(NWPlayer player, NWObject questSource, int questID)
        {
            _visibility.AdjustVisibility(player, questSource, false);
        }
    }
}
