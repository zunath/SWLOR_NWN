using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BreastplateI: AbstractQuest
    {
        public BreastplateI()
        {
            CreateQuest(122, "Armorsmith Guild Task: 1x Breastplate I", "arm_tsk_122")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "breastplate_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
