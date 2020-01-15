using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIIBlue: AbstractQuest
    {
        public TrainingFoilIIIBlue()
        {
            CreateQuest(537, "Engineering Guild Task: 1x Training Foil III (Blue)", "eng_tsk_537")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_3", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
