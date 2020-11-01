using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class MediumBlade: AbstractQuest
    {
        public MediumBlade()
        {
            CreateQuest(242, "Weaponsmith Guild Task: 1x Medium Blade", "wpn_tsk_242")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "medium_blade", 1, true)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 5);
        }
    }
}
