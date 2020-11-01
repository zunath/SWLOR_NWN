using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicForceRobes: AbstractQuest
    {
        public BasicForceRobes()
        {
            CreateQuest(104, "Armorsmith Guild Task: 1x Basic Force Robes", "arm_tsk_104")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_robe_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
