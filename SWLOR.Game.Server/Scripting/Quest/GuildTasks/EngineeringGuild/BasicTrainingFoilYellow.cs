using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilYellow: AbstractQuest
    {
        public BasicTrainingFoilYellow()
        {
            CreateQuest(370, "Engineering Guild Task: 1x Basic Training Foil (Yellow)", "eng_tsk_370")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_y_b", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
