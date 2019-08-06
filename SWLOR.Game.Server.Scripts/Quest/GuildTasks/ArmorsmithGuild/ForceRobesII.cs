using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
