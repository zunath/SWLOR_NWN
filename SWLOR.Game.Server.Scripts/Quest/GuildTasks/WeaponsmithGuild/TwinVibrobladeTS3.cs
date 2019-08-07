using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeTS3: AbstractQuest
    {
        public TwinVibrobladeTS3()
        {
            CreateQuest(318, "Weaponsmith Guild Task: 1x Twin Vibroblade TS3", "wpn_tsk_318")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "twinblade_3", 1, true)

                .AddRewardGold(305)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 64);
        }
    }
}
