using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIVBlue: AbstractQuest
    {
        public TrainingFoilStaffIVBlue()
        {
            CreateQuest(562, "Engineering Guild Task: 1x Training Foil Staff IV (Blue)", "eng_tsk_562")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_4", 1, true)

                .AddRewardGold(525)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 110);
        }
    }
}
