using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class SmallShieldIII: AbstractQuest
    {
        public SmallShieldIII()
        {
            CreateQuest(191, "Armorsmith Guild Task: 1x Small Shield III", "arm_tsk_191")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "small_shield_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
