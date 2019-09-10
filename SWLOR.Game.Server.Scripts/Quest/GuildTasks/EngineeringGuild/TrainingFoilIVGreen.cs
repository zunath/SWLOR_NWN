using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIVGreen: AbstractQuest
    {
        public TrainingFoilIVGreen()
        {
            CreateQuest(559, "Engineering Guild Task: 1x Training Foil IV (Green)", "eng_tsk_559")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_g_4", 1, true)

                .AddRewardGold(505)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 105);
        }
    }
}
