using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBootsIV: AbstractQuest
    {
        public ForceBootsIV()
        {
            CreateQuest(197, "Armorsmith Guild Task: 1x Force Boots IV", "arm_tsk_197")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
