using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilStaffRed: AbstractQuest
    {
        public BasicTrainingFoilStaffRed()
        {
            CreateQuest(373, "Engineering Guild Task: 1x Basic Training Foil Staff (Red)", "eng_tsk_373")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_r_b", 1, true)

                .AddRewardGold(125)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
