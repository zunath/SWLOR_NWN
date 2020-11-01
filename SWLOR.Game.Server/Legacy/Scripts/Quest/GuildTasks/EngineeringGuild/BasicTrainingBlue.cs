using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingBlue: AbstractQuest
    {
        public BasicTrainingBlue()
        {
            CreateQuest(371, "Engineering Guild Task: 1x Basic Training  (Blue)", "eng_tsk_371")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_b", 1, true)

                .AddRewardGold(125)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
