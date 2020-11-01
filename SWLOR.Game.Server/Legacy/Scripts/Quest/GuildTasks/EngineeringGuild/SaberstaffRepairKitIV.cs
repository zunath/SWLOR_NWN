using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SaberstaffRepairKitIV: AbstractQuest
    {
        public SaberstaffRepairKitIV()
        {
            CreateQuest(551, "Engineering Guild Task: 1x Saberstaff Repair Kit IV", "eng_tsk_551")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ss_rep_4", 1, true)

                .AddRewardGold(455)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 97);
        }
    }
}
