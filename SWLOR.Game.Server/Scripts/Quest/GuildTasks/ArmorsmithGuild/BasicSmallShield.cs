using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicSmallShield: AbstractQuest
    {
        public BasicSmallShield()
        {
            CreateQuest(112, "Armorsmith Guild Task: 1x Basic Small Shield", "arm_tsk_112")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_shield_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
