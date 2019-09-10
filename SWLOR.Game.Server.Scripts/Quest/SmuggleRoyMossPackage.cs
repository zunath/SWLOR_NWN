using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class SmuggleRoyMossPackage: AbstractQuest
    {
        public SmuggleRoyMossPackage()
        {
            CreateQuest(23, "Smuggle Roy Moss's Package", "smuggle_roy_moss")
                
                .AddObjectiveTalkToNPC(1)

                .AddRewardFame(3, 25)
                
                .OnAccepted((player, o) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 18);
                })
                
                .OnCompleted((player, o) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, 18);
                });
        }
    }
}
