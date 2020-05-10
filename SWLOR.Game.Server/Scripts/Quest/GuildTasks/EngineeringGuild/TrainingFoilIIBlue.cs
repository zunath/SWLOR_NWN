using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIIBlue: AbstractQuest
    {
        public TrainingFoilIIBlue()
        {
            CreateQuest(479, "Engineering Guild Task: 1x Training Foil II (Blue)", "eng_tsk_479")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_2", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
