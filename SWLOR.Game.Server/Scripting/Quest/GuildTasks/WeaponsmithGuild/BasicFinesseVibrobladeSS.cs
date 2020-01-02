using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
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
