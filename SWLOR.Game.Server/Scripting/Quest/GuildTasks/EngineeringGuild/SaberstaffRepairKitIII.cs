using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class SaberstaffRepairKitIII: AbstractQuest
    {
        public SaberstaffRepairKitIII()
        {
            CreateQuest(493, "Engineering Guild Task: 1x Saberstaff Repair Kit III", "eng_tsk_493")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 3)


                .AddObjectiveCollectItem(1, "ss_rep_3", 1, true)

                .AddRewardGold(355)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 77);
        }
    }
}
