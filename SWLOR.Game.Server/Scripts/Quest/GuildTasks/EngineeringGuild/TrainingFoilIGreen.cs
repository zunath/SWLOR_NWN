using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIGreen: AbstractQuest
    {
        public TrainingFoilIGreen()
        {
            CreateQuest(430, "Engineering Guild Task: 1x Training Foil I (Green)", "eng_tsk_430")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_g_1", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
