using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIBlue: AbstractQuest
    {
        public TrainingFoilStaffIBlue()
        {
            CreateQuest(441, "Engineering Guild Task: 1x Training Foil Staff I (Blue)", "eng_tsk_441")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_1", 1, true)

                .AddRewardGold(225)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 50);
        }
    }
}
