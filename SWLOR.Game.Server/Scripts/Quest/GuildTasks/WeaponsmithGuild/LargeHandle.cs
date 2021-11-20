using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class LargeHandle: AbstractQuest
    {
        public LargeHandle()
        {
            CreateQuest(241, "Weaponsmith Guild Task: 1x Large Handle", "wpn_tsk_241")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_handle", 1, true)

                .AddRewardGold(80)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 20);
        }
    }
}
