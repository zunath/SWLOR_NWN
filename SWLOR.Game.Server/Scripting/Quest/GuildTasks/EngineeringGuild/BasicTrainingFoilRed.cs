using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilRed: AbstractQuest
    {
        public BasicTrainingFoilRed()
        {
            CreateQuest(369, "Engineering Guild Task: 1x Basic Training Foil (Red)", "eng_tsk_369")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 99)


                .AddObjectiveCollectItem(1, "lightsaber_r_b", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
