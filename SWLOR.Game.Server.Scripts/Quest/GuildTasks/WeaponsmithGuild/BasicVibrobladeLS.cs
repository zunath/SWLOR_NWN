using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicVibrobladeLS: AbstractQuest
    {
        public BasicVibrobladeLS()
        {
            CreateQuest(239, "Weaponsmith Guild Task: 1x Basic Vibroblade LS", "wpn_tsk_239")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "longsword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
