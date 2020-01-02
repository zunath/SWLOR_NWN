using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmS1: AbstractQuest
    {
        public PolearmS1()
        {
            CreateQuest(264, "Weaponsmith Guild Task: 1x Polearm S1", "wpn_tsk_264")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "spear_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
