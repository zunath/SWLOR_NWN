using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceHelmetI: AbstractQuest
    {
        public ForceHelmetI()
        {
            CreateQuest(126, "Armorsmith Guild Task: 1x Force Helmet I", "arm_tsk_126")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_f1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
