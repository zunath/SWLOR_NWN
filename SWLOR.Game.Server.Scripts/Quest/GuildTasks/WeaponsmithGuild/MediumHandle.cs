using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class MediumHandle: AbstractQuest
    {
        public MediumHandle()
        {
            CreateQuest(243, "Weaponsmith Guild Task: 1x Medium Handle", "wpn_tsk_243")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "medium_handle", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 15);
        }
    }
}
