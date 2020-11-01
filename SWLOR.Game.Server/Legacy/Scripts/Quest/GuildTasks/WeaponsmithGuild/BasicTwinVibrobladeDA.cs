using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicTwinVibrobladeDA: AbstractQuest
    {
        public BasicTwinVibrobladeDA()
        {
            CreateQuest(234, "Weaponsmith Guild Task: 1x Basic Twin Vibroblade DA", "wpn_tsk_234")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "doubleaxe_b", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 14);
        }
    }
}
