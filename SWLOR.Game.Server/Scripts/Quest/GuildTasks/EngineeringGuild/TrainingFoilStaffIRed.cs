using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIRed: AbstractQuest
    {
        public TrainingFoilStaffIRed()
        {
            CreateQuest(443, "Engineering Guild Task: 1x Training Foil Staff I (Red)", "eng_tsk_443")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_r_1", 1, true)

                .AddRewardGold(225)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 50);
        }
    }
}
