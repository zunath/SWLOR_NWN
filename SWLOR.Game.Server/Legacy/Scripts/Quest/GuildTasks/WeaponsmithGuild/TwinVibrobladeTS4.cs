using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeTS4: AbstractQuest
    {
        public TwinVibrobladeTS4()
        {
            CreateQuest(343, "Weaponsmith Guild Task: 1x Twin Vibroblade TS4", "wpn_tsk_343")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "twinblade_4", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 84);
        }
    }
}
