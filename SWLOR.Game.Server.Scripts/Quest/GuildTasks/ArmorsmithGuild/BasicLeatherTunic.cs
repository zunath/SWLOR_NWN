using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicLeatherTunic: AbstractQuest
    {
        public BasicLeatherTunic()
        {
            CreateQuest(108, "Armorsmith Guild Task: 1x Basic Leather Tunic", "arm_tsk_108")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "leather_tunic_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
