using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeTS2: AbstractQuest
    {
        public TwinVibrobladeTS2()
        {
            CreateQuest(293, "Weaponsmith Guild Task: 1x Twin Vibroblade TS2", "wpn_tsk_293")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "twinblade_2", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 44);
        }
    }
}
