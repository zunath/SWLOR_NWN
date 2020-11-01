using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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
