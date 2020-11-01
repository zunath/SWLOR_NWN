using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceNecklaceIII: AbstractQuest
    {
        public ForceNecklaceIII()
        {
            CreateQuest(176, "Armorsmith Guild Task: 1x Force Necklace III", "arm_tsk_176")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_neck_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
