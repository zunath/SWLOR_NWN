using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LargeShieldIII: AbstractQuest
    {
        public LargeShieldIII()
        {
            CreateQuest(182, "Armorsmith Guild Task: 1x Large Shield III", "arm_tsk_182")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_shield_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
