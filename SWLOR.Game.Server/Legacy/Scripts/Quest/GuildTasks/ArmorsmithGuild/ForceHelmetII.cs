using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceHelmetII: AbstractQuest
    {
        public ForceHelmetII()
        {
            CreateQuest(149, "Armorsmith Guild Task: 1x Force Helmet II", "arm_tsk_149")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_f2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
