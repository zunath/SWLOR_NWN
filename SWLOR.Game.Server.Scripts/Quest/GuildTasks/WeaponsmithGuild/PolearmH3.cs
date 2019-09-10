using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmH3: AbstractQuest
    {
        public PolearmH3()
        {
            CreateQuest(312, "Weaponsmith Guild Task: 1x Polearm H3", "wpn_tsk_312")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "halberd_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
