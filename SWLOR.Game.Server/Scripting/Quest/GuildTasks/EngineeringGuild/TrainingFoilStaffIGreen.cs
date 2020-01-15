using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIGreen: AbstractQuest
    {
        public TrainingFoilStaffIGreen()
        {
            CreateQuest(442, "Engineering Guild Task: 1x Training Foil Staff I (Green)", "eng_tsk_442")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_g_1", 1, true)

                .AddRewardGold(225)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 50);
        }
    }
}
