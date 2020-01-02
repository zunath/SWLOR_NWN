using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilGreen: AbstractQuest
    {
        public BasicTrainingFoilGreen()
        {
            CreateQuest(368, "Engineering Guild Task: 1x Basic Training Foil (Green)", "eng_tsk_368")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_g_b", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
