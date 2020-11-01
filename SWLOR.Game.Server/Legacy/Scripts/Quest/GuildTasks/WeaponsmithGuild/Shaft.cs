using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class Shaft: AbstractQuest
    {
        public Shaft()
        {
            CreateQuest(245, "Weaponsmith Guild Task: 1x Shaft", "wpn_tsk_245")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shaft", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 15);
        }
    }
}
