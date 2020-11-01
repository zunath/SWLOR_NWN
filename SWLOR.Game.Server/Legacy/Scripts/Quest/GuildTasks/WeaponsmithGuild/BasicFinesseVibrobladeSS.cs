using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicFinesseVibrobladeSS: AbstractQuest
    {
        public BasicFinesseVibrobladeSS()
        {
            CreateQuest(228, "Weaponsmith Guild Task: 1x Basic Finesse Vibroblade SS", "wpn_tsk_228")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shortsword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
