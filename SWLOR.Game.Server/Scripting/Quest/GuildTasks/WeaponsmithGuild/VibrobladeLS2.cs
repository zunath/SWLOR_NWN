using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeLS2: AbstractQuest
    {
        public VibrobladeLS2()
        {
            CreateQuest(297, "Weaponsmith Guild Task: 1x Vibroblade LS2", "wpn_tsk_297")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "longsword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
