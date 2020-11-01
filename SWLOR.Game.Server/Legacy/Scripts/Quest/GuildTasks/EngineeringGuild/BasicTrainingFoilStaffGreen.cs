using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilStaffGreen: AbstractQuest
    {
        public BasicTrainingFoilStaffGreen()
        {
            CreateQuest(372, "Engineering Guild Task: 1x Basic Training Foil Staff (Green)", "eng_tsk_372")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_g_b", 1, true)

                .AddRewardGold(125)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
