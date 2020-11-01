using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
