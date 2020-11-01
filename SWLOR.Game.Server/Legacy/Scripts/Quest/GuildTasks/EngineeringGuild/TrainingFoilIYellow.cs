using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIYellow: AbstractQuest
    {
        public TrainingFoilIYellow()
        {
            CreateQuest(432, "Engineering Guild Task: 1x Training Foil I (Yellow)", "eng_tsk_432")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_y_1", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
