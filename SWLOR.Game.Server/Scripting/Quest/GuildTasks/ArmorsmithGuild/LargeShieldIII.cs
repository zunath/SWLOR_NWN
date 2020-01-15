using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LargeShieldIII: AbstractQuest
    {
        public LargeShieldIII()
        {
            CreateQuest(182, "Armorsmith Guild Task: 1x Large Shield III", "arm_tsk_182")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 3)


                .AddObjectiveCollectItem(1, "large_shield_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
