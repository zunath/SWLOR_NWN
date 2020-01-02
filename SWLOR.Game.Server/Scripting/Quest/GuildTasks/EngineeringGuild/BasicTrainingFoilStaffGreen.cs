using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
