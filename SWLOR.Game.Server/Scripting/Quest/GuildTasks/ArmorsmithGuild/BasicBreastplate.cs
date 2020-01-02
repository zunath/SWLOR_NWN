using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicBreastplate: AbstractQuest
    {
        public BasicBreastplate()
        {
            CreateQuest(101, "Armorsmith Guild Task: 1x Basic Breastplate", "arm_tsk_101")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "breastplate_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
