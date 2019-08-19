using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class StinkyWomprats : AbstractQuest
    {
        public StinkyWomprats()
        {
            CreateQuest(32, "Stinky Womprats", "stinky_womprats")           
                .AddObjectiveKillTarget(1, NPCGroupType.Tatooine_Womprat, 20)
                .AddObjectiveTalkToNPC(2)
                .AddRewardGold(1000)
                .AddRewardItem("xp_tome_4", 1);
        }
    }
}
