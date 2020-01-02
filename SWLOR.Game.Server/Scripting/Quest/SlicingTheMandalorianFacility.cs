using System.Linq;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class SlicingTheMandalorianFacility: AbstractQuest
    {
        public SlicingTheMandalorianFacility()
        {
            CreateQuest(22, "Slicing the Mandalorian Facility", "mandalorian_slicing")
                .AddPrerequisiteQuest(20)
                .AddPrerequisiteQuest(21)

                .AddObjectiveUseObject(1)

                .AddRewardGold(550)
                .AddRewardFame(3, 25)
                .AddRewardItem("xp_tome_1", 1)
                
                .OnAccepted((player, questSource) =>
                {
                    string[] visibilityObjectIDs =
                    {
                        "C1888BC5BBBC45F28B40293D7C6E76EC",
                        "C3F31641D4F34D6AAEA51295CBE9014D",
                        "6FABDF6EDF4F47A4A9684E6224700A78",
                        "5B56B9EF160D4B078E28C775723BA95F",
                        "141D32140AA847B18AD5896C82223C8D",
                        "B0839B0F597140EEAEC567C22FFD1A86"
                    };

                    foreach(var objID in visibilityObjectIDs)
                    {
                        var obj = AppCache.VisibilityObjects.Single(x => x.Key == objID).Value;
                        ObjectVisibilityService.AdjustVisibility(player, obj, true);
                    }
                })
                
                .OnCompleted((player, questSource) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, 12);
                    KeyItemService.RemovePlayerKeyItem(player, 13);
                    KeyItemService.RemovePlayerKeyItem(player, 14);
                    KeyItemService.RemovePlayerKeyItem(player, 15);
                    KeyItemService.RemovePlayerKeyItem(player, 16);
                    KeyItemService.RemovePlayerKeyItem(player, 17);
                });

        }
    }
}
