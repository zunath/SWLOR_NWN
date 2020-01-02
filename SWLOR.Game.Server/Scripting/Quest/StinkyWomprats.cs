using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class StinkyWomprats : AbstractQuest
    {
        public StinkyWomprats()
        {
            CreateQuest(33, "Stinky Womprats", "stinky_womprats")           
                .AddObjectiveCollectItem(1, "womprathide", 10, false)
                .AddObjectiveTalkToNPC(2)
                .AddRewardGold(1000)
                .AddRewardItem("xp_tome_4", 1);
        }
    }
}
