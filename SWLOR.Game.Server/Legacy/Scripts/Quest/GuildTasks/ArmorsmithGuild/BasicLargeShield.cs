using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicLargeShield: AbstractQuest
    {
        public BasicLargeShield()
        {
            CreateQuest(107, "Armorsmith Guild Task: 1x Basic Large Shield", "arm_tsk_107")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "large_shield_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
