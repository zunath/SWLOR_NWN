using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class SmallBlade: AbstractQuest
    {
        public SmallBlade()
        {
            CreateQuest(246, "Weaponsmith Guild Task: 1x Small Blade", "wpn_tsk_246")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_blade", 1, true)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 5);
        }
    }
}
