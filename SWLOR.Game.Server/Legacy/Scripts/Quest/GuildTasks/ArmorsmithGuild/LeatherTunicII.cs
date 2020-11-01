using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LeatherTunicII: AbstractQuest
    {
        public LeatherTunicII()
        {
            CreateQuest(159, "Armorsmith Guild Task: 1x Leather Tunic II", "arm_tsk_159")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "leather_tunic_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
