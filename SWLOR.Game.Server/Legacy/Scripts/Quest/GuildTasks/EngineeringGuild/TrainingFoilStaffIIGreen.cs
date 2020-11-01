using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIGreen: AbstractQuest
    {
        public TrainingFoilStaffIIGreen()
        {
            CreateQuest(490, "Engineering Guild Task: 1x Training Foil Staff II (Green)", "eng_tsk_490")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_g_2", 1, true)

                .AddRewardGold(325)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 70);
        }
    }
}
