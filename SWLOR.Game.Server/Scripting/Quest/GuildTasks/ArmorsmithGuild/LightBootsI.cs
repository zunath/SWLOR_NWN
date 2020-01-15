using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsI: AbstractQuest
    {
        public LightBootsI()
        {
            CreateQuest(138, "Armorsmith Guild Task: 1x Light Boots I", "arm_tsk_138")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 1)


                .AddObjectiveCollectItem(1, "light_boots_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
