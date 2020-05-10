using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SaberstaffRepairKitII: AbstractQuest
    {
        public SaberstaffRepairKitII()
        {
            CreateQuest(445, "Engineering Guild Task: 1x Saberstaff Repair Kit II", "eng_tsk_445")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ss_rep_2", 1, true)

                .AddRewardGold(255)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 57);
        }
    }
}
