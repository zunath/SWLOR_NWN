using System.Linq;
using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
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
                .AddRewardFame(3, 15)
                
                .OnAccepted((player, questSource) =>
                {
                    var obj = AppCache.VisibilityObjects.Single(x => x.Key == "A61BB617B2D34E2F863C6301A4A04143").Value;
                    ObjectVisibilityService.AdjustVisibility(player, obj, true);
                });
        }
    }
}
