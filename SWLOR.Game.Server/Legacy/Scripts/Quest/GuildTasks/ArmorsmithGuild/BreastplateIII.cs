using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BreastplateIII: AbstractQuest
    {
        public BreastplateIII()
        {
            CreateQuest(171, "Armorsmith Guild Task: 1x Breastplate III", "arm_tsk_171")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "breastplate_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
