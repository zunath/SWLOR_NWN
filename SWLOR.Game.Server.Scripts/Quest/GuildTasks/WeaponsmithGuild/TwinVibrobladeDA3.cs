using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeDA3: AbstractQuest
    {
        public TwinVibrobladeDA3()
        {
            CreateQuest(316, "Weaponsmith Guild Task: 1x Twin Vibroblade DA3", "wpn_tsk_316")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "doubleaxe_3", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 64);
        }
    }
}
