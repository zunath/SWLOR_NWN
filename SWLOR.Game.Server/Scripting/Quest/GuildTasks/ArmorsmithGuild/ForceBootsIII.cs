using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBootsIII: AbstractQuest
    {
        public ForceBootsIII()
        {
            CreateQuest(174, "Armorsmith Guild Task: 1x Force Boots III", "arm_tsk_174")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
