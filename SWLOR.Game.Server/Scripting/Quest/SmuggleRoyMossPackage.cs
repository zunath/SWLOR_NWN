using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class SmuggleRoyMossPackage: AbstractQuest
    {
        public SmuggleRoyMossPackage()
        {
            CreateQuest(23, "Smuggle Roy Moss's Package", "smuggle_roy_moss")
                
                .AddObjectiveTalkToNPC(1)

                .AddRewardFame(FameRegion.VelesColony, 25)
                
                .OnAccepted((player, o) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, KeyItem.PackageForDenamReyholm);
                })
                
                .OnCompleted((player, o) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, KeyItem.PackageForDenamReyholm);
                });
        }
    }
}
