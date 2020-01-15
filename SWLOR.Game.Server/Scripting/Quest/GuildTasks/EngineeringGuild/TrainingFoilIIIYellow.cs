using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIIYellow: AbstractQuest
    {
        public TrainingFoilIIIYellow()
        {
            CreateQuest(540, "Engineering Guild Task: 1x Training Foil III (Yellow)", "eng_tsk_540")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_y_3", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
