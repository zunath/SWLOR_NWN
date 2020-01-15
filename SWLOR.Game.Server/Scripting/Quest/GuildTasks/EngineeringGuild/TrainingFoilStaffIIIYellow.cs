using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIIYellow: AbstractQuest
    {
        public TrainingFoilStaffIIIYellow()
        {
            CreateQuest(550, "Engineering Guild Task: 1x Training Foil Staff III (Yellow)", "eng_tsk_550")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_y_3", 1, true)

                .AddRewardGold(425)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
