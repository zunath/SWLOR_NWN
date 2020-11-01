using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LargeShieldI: AbstractQuest
    {
        public LargeShieldI()
        {
            CreateQuest(134, "Armorsmith Guild Task: 1x Large Shield I", "arm_tsk_134")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_shield_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
