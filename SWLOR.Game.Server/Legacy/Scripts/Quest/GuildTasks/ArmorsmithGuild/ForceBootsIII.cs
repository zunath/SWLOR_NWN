using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBootsIII: AbstractQuest
    {
        public ForceBootsIII()
        {
            CreateQuest(174, "Armorsmith Guild Task: 1x Force Boots III", "arm_tsk_174")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
