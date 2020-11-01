using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicForceBoots: AbstractQuest
    {
        public BasicForceBoots()
        {
            CreateQuest(102, "Armorsmith Guild Task: 1x Basic Force Boots", "arm_tsk_102")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "force_boots_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
