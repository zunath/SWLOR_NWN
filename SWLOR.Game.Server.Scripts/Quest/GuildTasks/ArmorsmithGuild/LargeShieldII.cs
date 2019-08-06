using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LargeShieldII: AbstractQuest
    {
        public LargeShieldII()
        {
            CreateQuest(158, "Armorsmith Guild Task: 1x Large Shield II", "arm_tsk_158")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_shield_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
