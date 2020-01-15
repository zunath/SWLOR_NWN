using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsIII: AbstractQuest
    {
        public LightBootsIII()
        {
            CreateQuest(186, "Armorsmith Guild Task: 1x Light Boots III", "arm_tsk_186")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 3)


                .AddObjectiveCollectItem(1, "light_boots_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
