using System.Linq;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class CoxxionInitiation: AbstractQuest
    {
        public CoxxionInitiation()
        {
            CreateQuest(25, "Coxxion Initiation", "caxx_init")

                .AddObjectiveTalkToNPC(1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(150)
                .AddRewardFame(4, 20)
                
                .OnAccepted((player, questSource) =>
                {
                    var obj = AppCache.VisibilityObjects.Single(x => x.Key == "FF65A192706B40A6A97474B935796B82").Value;
                    ObjectVisibilityService.AdjustVisibility(player, obj, true);

                })
                
                .OnAdvanced((player, questSource) =>
                {
                    ObjectVisibilityService.AdjustVisibility(player, questSource, false);
                })
                
                .OnCompleted((player, questSource) =>
                {
                    var obj = AppCache.VisibilityObjects.Single(x => x.Key == "D4C44145731048F1B7DA23D974E59FCE").Value;
                    ObjectVisibilityService.AdjustVisibility(player, obj, true);
                });
        }
    }
}
