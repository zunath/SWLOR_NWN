using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeLS4: AbstractQuest
    {
        public VibrobladeLS4()
        {
            CreateQuest(347, "Weaponsmith Guild Task: 1x Vibroblade LS4", "wpn_tsk_347")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "longsword_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
