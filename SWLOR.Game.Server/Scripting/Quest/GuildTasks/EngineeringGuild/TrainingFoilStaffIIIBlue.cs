using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIIBlue: AbstractQuest
    {
        public TrainingFoilStaffIIIBlue()
        {
            CreateQuest(547, "Engineering Guild Task: 1x Training Foil Staff III (Blue)", "eng_tsk_547")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_3", 1, true)

                .AddRewardGold(425)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
