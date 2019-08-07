using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIIRed: AbstractQuest
    {
        public TrainingFoilStaffIIIRed()
        {
            CreateQuest(549, "Engineering Guild Task: 1x Training Foil Staff III (Red)", "eng_tsk_549")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_r_3", 1, true)

                .AddRewardGold(425)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
