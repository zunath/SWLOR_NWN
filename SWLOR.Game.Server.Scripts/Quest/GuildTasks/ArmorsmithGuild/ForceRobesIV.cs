using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceRobesIV: AbstractQuest
    {
        public ForceRobesIV()
        {
            CreateQuest(200, "Armorsmith Guild Task: 1x Force Robes IV", "arm_tsk_200")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_robe_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
