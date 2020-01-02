using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeLS1: AbstractQuest
    {
        public VibrobladeLS1()
        {
            CreateQuest(272, "Weaponsmith Guild Task: 1x Vibroblade LS1", "wpn_tsk_272")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "longsword_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
