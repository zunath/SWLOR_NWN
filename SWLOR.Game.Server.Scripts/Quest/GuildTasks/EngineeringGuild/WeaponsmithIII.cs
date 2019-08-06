using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class WeaponsmithIII: AbstractQuest
    {
        public WeaponsmithIII()
        {
            CreateQuest(556, "Engineering Guild Task: 1x Weaponsmith III", "eng_tsk_556")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_wpnsmth3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
