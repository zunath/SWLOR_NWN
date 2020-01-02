using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIVRed: AbstractQuest
    {
        public TrainingFoilStaffIVRed()
        {
            CreateQuest(564, "Engineering Guild Task: 1x Training Foil Staff IV (Red)", "eng_tsk_564")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_r_4", 1, true)

                .AddRewardGold(525)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 110);
        }
    }
}
