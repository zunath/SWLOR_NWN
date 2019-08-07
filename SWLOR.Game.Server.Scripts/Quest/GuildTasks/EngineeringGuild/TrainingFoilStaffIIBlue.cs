using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIIBlue: AbstractQuest
    {
        public TrainingFoilStaffIIBlue()
        {
            CreateQuest(489, "Engineering Guild Task: 1x Training Foil Staff II (Blue)", "eng_tsk_489")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_2", 1, true)

                .AddRewardGold(325)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 70);
        }
    }
}
