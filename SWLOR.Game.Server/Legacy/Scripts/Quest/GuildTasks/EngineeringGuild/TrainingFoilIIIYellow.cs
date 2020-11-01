using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIIYellow: AbstractQuest
    {
        public TrainingFoilIIIYellow()
        {
            CreateQuest(540, "Engineering Guild Task: 1x Training Foil III (Yellow)", "eng_tsk_540")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_y_3", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
