using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIIGreen: AbstractQuest
    {
        public TrainingFoilIIIGreen()
        {
            CreateQuest(538, "Engineering Guild Task: 1x Training Foil III (Green)", "eng_tsk_538")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_g_3", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
