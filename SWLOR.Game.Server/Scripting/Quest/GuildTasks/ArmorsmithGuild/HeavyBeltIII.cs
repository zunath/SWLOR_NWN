using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBeltIII: AbstractQuest
    {
        public HeavyBeltIII()
        {
            CreateQuest(179, "Armorsmith Guild Task: 1x Heavy Belt III", "arm_tsk_179")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 3)


                .AddObjectiveCollectItem(1, "heavy_belt_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
