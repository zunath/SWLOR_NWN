using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIRed: AbstractQuest
    {
        public TrainingFoilStaffIIRed()
        {
            CreateQuest(491, "Engineering Guild Task: 1x Training Foil Staff II (Red)", "eng_tsk_491")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_r_2", 1, true)

                .AddRewardGold(325)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 70);
        }
    }
}
