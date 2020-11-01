using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilBlue: AbstractQuest
    {
        public BasicTrainingFoilBlue()
        {
            CreateQuest(367, "Engineering Guild Task: 1x Basic Training Foil (Blue)", "eng_tsk_367")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_b", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
