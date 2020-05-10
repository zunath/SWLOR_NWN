using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicPolearmS: AbstractQuest
    {
        public BasicPolearmS()
        {
            CreateQuest(232, "Weaponsmith Guild Task: 1x Basic Polearm S", "wpn_tsk_232")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "spear_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
