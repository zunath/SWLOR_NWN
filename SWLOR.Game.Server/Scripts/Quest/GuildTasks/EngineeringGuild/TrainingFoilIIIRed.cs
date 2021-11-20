using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIIRed: AbstractQuest
    {
        public TrainingFoilIIIRed()
        {
            CreateQuest(539, "Engineering Guild Task: 1x Training Foil III (Red)", "eng_tsk_539")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_r_3", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
