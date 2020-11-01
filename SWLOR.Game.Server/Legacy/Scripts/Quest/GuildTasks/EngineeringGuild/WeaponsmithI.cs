using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class WeaponsmithI: AbstractQuest
    {
        public WeaponsmithI()
        {
            CreateQuest(409, "Engineering Guild Task: 1x Weaponsmith I", "eng_tsk_409")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_wpnsmth1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
