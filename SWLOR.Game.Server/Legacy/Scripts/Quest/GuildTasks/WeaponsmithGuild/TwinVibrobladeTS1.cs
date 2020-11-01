using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeTS1: AbstractQuest
    {
        public TwinVibrobladeTS1()
        {
            CreateQuest(268, "Weaponsmith Guild Task: 1x Twin Vibroblade TS1", "wpn_tsk_268")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "twinblade_1", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 24);
        }
    }
}
