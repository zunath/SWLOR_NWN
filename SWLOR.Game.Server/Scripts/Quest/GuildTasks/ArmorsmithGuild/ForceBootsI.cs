using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceBootsI: AbstractQuest
    {
        public ForceBootsI()
        {
            CreateQuest(125, "Armorsmith Guild Task: 1x Force Boots I", "arm_tsk_125")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
