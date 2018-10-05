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
        private readonly AppState _state;

        public ShowQuestObjectRule(
            IObjectVisibilityService ovs,
            AppState state)
        {
            _ovs = ovs;
            _state = state;
        }


        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int count = args.Length;

            for (int index = 0; index < count; index++)
            {
                string visibilityObjectID = args[index];

                if (string.IsNullOrWhiteSpace(visibilityObjectID)) return;

                var obj = _state.VisibilityObjects.Single(x => x.Key == visibilityObjectID).Value;
                _ovs.AdjustVisibility(player, obj, true);
            }
            
        }
    }
}
