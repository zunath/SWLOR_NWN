using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIIGreen: AbstractQuest
    {
        public TrainingFoilStaffIIIGreen()
        {
            CreateQuest(548, "Engineering Guild Task: 1x Training Foil Staff III (Green)", "eng_tsk_548")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_g_3", 1, true)

                .AddRewardGold(425)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
