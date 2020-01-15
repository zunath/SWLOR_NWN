using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class SaberstaffRepairKitI: AbstractQuest
    {
        public SaberstaffRepairKitI()
        {
            CreateQuest(406, "Engineering Guild Task: 1x Saberstaff Repair Kit I", "eng_tsk_406")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 1)


                .AddObjectiveCollectItem(1, "ss_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 37);
        }
    }
}
