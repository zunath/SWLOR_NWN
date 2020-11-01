using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LeatherTunicIII: AbstractQuest
    {
        public LeatherTunicIII()
        {
            CreateQuest(183, "Armorsmith Guild Task: 1x Leather Tunic III", "arm_tsk_183")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "leather_tunic_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
