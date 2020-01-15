using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIRed: AbstractQuest
    {
        public TrainingFoilIIRed()
        {
            CreateQuest(481, "Engineering Guild Task: 1x Training Foil II (Red)", "eng_tsk_481")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_r_2", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
