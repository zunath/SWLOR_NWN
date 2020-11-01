using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeDA1: AbstractQuest
    {
        public TwinVibrobladeDA1()
        {
            CreateQuest(266, "Weaponsmith Guild Task: 1x Twin Vibroblade DA1", "wpn_tsk_266")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "doubleaxe_1", 1, true)

                .AddRewardGold(105)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 24);
        }
    }
}
