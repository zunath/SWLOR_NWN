using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilYellow: AbstractQuest
    {
        public BasicTrainingFoilYellow()
        {
            CreateQuest(370, "Engineering Guild Task: 1x Basic Training Foil (Yellow)", "eng_tsk_370")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_y_b", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
