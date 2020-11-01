using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicTwinVibrobladeTS: AbstractQuest
    {
        public BasicTwinVibrobladeTS()
        {
            CreateQuest(235, "Weaponsmith Guild Task: 1x Basic Twin Vibroblade TS", "wpn_tsk_235")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "twinblade_b", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 14);
        }
    }
}
