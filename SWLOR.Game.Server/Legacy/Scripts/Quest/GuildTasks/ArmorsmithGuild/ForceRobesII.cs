using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceRobesII: AbstractQuest
    {
        public ForceRobesII()
        {
            CreateQuest(151, "Armorsmith Guild Task: 1x Force Robes II", "arm_tsk_151")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_robe_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
