using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class FirstRites: AbstractQuest
    {
        public FirstRites()
        {
            CreateQuest(30, "First Rites", "first_rites")
                .AddObjectiveUseObject(1)
                .AddObjectiveUseObject(2)

                .AddRewardFame(FameRegion.Global, 10)
                
                .OnAccepted((player, questSource) =>
                {
                    var obj = AppCache.VisibilityObjects.Single(x => x.Key == "81533EBB-2084-4C97-B004-8E1D8C395F56").Value;
                    ObjectVisibilityService.AdjustVisibility(player, obj, true);
                })
                
                .OnAdvanced((player, questSource, state) =>
                {
                    ObjectVisibilityService.AdjustVisibility(player, questSource, false);
                });
        }
    }
}
