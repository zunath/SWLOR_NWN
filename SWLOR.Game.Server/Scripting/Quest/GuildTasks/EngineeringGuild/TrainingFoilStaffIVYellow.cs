using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIVYellow: AbstractQuest
    {
        public TrainingFoilStaffIVYellow()
        {
            CreateQuest(565, "Engineering Guild Task: 1x Training Foil Staff IV (Yellow)", "eng_tsk_565")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "saberstaff_y_4", 1, true)

                .AddRewardGold(525)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 110);
        }
    }
}
