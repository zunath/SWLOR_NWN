using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class SmallHandle: AbstractQuest
    {
        public SmallHandle()
        {
            CreateQuest(247, "Weaponsmith Guild Task: 1x Small Handle", "wpn_tsk_247")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_handle", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 10);
        }
    }
}
