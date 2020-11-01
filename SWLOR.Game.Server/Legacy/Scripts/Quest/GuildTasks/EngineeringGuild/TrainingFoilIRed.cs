using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIRed: AbstractQuest
    {
        public TrainingFoilIRed()
        {
            CreateQuest(431, "Engineering Guild Task: 1x Training Foil I (Red)", "eng_tsk_431")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_r_1", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
