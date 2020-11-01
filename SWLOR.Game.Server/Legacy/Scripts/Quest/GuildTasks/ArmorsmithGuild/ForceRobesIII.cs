using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceRobesIII: AbstractQuest
    {
        public ForceRobesIII()
        {
            CreateQuest(177, "Armorsmith Guild Task: 1x Force Robes III", "arm_tsk_177")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_robe_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
