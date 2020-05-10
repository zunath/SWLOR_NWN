using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIGreen: AbstractQuest
    {
        public TrainingFoilIIGreen()
        {
            CreateQuest(480, "Engineering Guild Task: 1x Training Foil II (Green)", "eng_tsk_480")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_g_2", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
