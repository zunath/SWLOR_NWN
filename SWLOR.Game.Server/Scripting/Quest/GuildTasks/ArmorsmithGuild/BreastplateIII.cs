using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BreastplateIII: AbstractQuest
    {
        public BreastplateIII()
        {
            CreateQuest(171, "Armorsmith Guild Task: 1x Breastplate III", "arm_tsk_171")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 3)


                .AddObjectiveCollectItem(1, "breastplate_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
