using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeK2: AbstractQuest
    {
        public VibrobladeK2()
        {
            CreateQuest(296, "Weaponsmith Guild Task: 1x Vibroblade K2", "wpn_tsk_296")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "katana_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
