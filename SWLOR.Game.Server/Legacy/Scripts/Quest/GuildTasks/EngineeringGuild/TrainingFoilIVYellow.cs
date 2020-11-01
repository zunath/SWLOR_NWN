using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIVYellow: AbstractQuest
    {
        public TrainingFoilIVYellow()
        {
            CreateQuest(561, "Engineering Guild Task: 1x Training Foil IV (Yellow)", "eng_tsk_561")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_y_4", 1, true)

                .AddRewardGold(505)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 105);
        }
    }
}
