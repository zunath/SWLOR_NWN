using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIIGreen: AbstractQuest
    {
        public TrainingFoilStaffIIIGreen()
        {
            CreateQuest(548, "Engineering Guild Task: 1x Training Foil Staff III (Green)", "eng_tsk_548")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_g_3", 1, true)

                .AddRewardGold(425)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
