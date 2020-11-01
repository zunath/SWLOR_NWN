using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceNecklaceIV: AbstractQuest
    {
        public ForceNecklaceIV()
        {
            CreateQuest(199, "Armorsmith Guild Task: 1x Force Necklace IV", "arm_tsk_199")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_neck_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
