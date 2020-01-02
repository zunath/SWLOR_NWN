using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeDA2: AbstractQuest
    {
        public TwinVibrobladeDA2()
        {
            CreateQuest(291, "Weaponsmith Guild Task: 1x Twin Vibroblade DA2", "wpn_tsk_291")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "doubleaxe_2", 1, true)

                .AddRewardGold(205)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 44);
        }
    }
}
