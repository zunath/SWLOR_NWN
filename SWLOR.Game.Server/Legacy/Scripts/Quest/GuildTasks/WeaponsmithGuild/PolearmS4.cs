using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmS4: AbstractQuest
    {
        public PolearmS4()
        {
            CreateQuest(339, "Weaponsmith Guild Task: 1x Polearm S4", "wpn_tsk_339")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "spear_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
