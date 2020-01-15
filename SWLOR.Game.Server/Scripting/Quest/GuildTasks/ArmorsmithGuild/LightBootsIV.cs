using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsIV: AbstractQuest
    {
        public LightBootsIV()
        {
            CreateQuest(209, "Armorsmith Guild Task: 1x Light Boots IV", "arm_tsk_209")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 4)


                .AddObjectiveCollectItem(1, "light_boots_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
