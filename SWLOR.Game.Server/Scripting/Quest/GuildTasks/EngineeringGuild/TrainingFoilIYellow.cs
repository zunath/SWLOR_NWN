using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIYellow: AbstractQuest
    {
        public TrainingFoilIYellow()
        {
            CreateQuest(432, "Engineering Guild Task: 1x Training Foil I (Yellow)", "eng_tsk_432")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_y_1", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
