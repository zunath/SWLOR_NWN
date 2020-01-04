using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest 
{
    public class WorkinForTheMan : AbstractQuest
    {
        public WorkinForTheMan()
        {
            CreateQuest(32, "Workin' for the Man", "workin_for_man")
                .IsRepeatable()
                .AddObjectiveKillTarget(1, NPCGroup.TuskenRaider, 20)
                .AddObjectiveTalkToNPC(2)
                .AddRewardGold(500)
                .AddRewardItem("xp_tome_4", 1);
        }
    }
}
