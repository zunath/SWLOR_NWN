using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class WeaponsmithII: AbstractQuest
    {
        public WeaponsmithII()
        {
            CreateQuest(497, "Engineering Guild Task: 1x Weaponsmith II", "eng_tsk_497")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_wpnsmth2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
