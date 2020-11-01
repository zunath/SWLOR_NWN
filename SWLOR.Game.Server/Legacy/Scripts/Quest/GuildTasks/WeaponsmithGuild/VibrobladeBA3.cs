using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBA3: AbstractQuest
    {
        public VibrobladeBA3()
        {
            CreateQuest(319, "Weaponsmith Guild Task: 1x Vibroblade BA3", "wpn_tsk_319")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "battleaxe_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
