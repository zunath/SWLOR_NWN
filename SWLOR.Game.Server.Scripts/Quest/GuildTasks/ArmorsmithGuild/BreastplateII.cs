using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BreastplateII: AbstractQuest
    {
        public BreastplateII()
        {
            CreateQuest(145, "Armorsmith Guild Task: 1x Breastplate II", "arm_tsk_145")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "breastplate_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
