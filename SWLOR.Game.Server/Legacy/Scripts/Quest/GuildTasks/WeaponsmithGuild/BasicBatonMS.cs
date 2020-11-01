using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicBatonMS: AbstractQuest
    {
        public BasicBatonMS()
        {
            CreateQuest(224, "Weaponsmith Guild Task: 1x Basic Baton MS", "wpn_tsk_224")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "morningstar_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
