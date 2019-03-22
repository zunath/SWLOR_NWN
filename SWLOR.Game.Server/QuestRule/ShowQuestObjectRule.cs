using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class ShowQuestObjectRule: IQuestRule
    {
        private readonly IObjectVisibilityService _ovs;
        
        public ShowQuestObjectRule(
            IObjectVisibilityService ovs)
        {
            _ovs = ovs;
        }


        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int count = args.Length;

            for (int index = 0; index < count; index++)
            {
                if (string.IsNullOrWhiteSpace(args[index])) return;

                string visibilityObjectID = args[index];

                var obj = AppCache.VisibilityObjects.Single(x => x.Key == visibilityObjectID).Value;
                _ovs.AdjustVisibility(player, obj, true);
            }
            
        }
    }
}
