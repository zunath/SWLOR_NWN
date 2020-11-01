using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicTrainingFoilStaffYellow: AbstractQuest
    {
        public BasicTrainingFoilStaffYellow()
        {
            CreateQuest(374, "Engineering Guild Task: 1x Basic Training Foil Staff (Yellow)", "eng_tsk_374")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_y_b", 1, true)

                .AddRewardGold(125)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
