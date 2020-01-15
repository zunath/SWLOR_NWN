using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIBlue: AbstractQuest
    {
        public TrainingFoilIBlue()
        {
            CreateQuest(429, "Engineering Guild Task: 1x Training Foil I (Blue)", "eng_tsk_429")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_1", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
