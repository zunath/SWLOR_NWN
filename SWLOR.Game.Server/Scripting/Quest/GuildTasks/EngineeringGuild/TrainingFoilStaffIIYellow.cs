using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIYellow: AbstractQuest
    {
        public TrainingFoilStaffIIYellow()
        {
            CreateQuest(492, "Engineering Guild Task: 1x Training Foil Staff II (Yellow)", "eng_tsk_492")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_y_2", 1, true)

                .AddRewardGold(325)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 70);
        }
    }
}
