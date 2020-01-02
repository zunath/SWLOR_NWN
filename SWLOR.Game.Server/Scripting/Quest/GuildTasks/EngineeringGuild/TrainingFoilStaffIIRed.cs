using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
