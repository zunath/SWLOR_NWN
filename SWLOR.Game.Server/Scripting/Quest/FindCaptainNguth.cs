using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class FindCaptainNguth: AbstractQuest
    {
        public FindCaptainNguth()
        {
            CreateQuest(17, "Find Captain N'Guth", "find_cap_nguth")
                .AddPrerequisiteQuest(16)

                .AddObjectiveUseObject(1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(300)
                .AddRewardFame(FameRegion.VelesColony, 15)
                
                .OnAccepted((player, questSource) =>
                {
                    var obj = AppCache.VisibilityObjects.Single(x => x.Key == "A61BB617B2D34E2F863C6301A4A04143").Value;
                    ObjectVisibilityService.AdjustVisibility(player, obj, true);
                });
        }
    }
}
