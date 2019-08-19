using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest 
{
    public class WorkinForTheMan : AbstractQuest
    {
        public WorkinForTheMan()
        {
            CreateQuest(33, "Workin' for the Man", "workin_for_man")
                .IsRepeatable()
                .AddObjectiveKillTarget(1, NPCGroupType.Tatooine_TuskenRaider, 20)
                .AddObjectiveTalkToNPC(2)
                .AddRewardGold(500)
                .AddRewardItem("xp_tome_4", 1);
        }
    }
}
