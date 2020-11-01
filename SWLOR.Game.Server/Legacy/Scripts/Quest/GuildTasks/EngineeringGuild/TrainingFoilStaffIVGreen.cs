using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilStaffIVGreen: AbstractQuest
    {
        public TrainingFoilStaffIVGreen()
        {
            CreateQuest(563, "Engineering Guild Task: 1x Training Foil Staff IV (Green)", "eng_tsk_563")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "saberstaff_g_4", 1, true)

                .AddRewardGold(525)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 110);
        }
    }
}
