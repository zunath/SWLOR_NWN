using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicVibrobladeBA: AbstractQuest
    {
        public BasicVibrobladeBA()
        {
            CreateQuest(236, "Weaponsmith Guild Task: 1x Basic Vibroblade BA", "wpn_tsk_236")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "battleaxe_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
