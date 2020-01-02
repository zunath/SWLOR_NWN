using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
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
