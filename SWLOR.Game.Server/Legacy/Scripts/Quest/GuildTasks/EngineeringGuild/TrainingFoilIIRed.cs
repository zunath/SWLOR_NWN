using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIRed: AbstractQuest
    {
        public TrainingFoilIIRed()
        {
            CreateQuest(481, "Engineering Guild Task: 1x Training Foil II (Red)", "eng_tsk_481")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_r_2", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
