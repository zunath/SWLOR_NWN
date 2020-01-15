using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightChokerI: AbstractQuest
    {
        public LightChokerI()
        {
            CreateQuest(139, "Armorsmith Guild Task: 1x Light Choker I", "arm_tsk_139")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 1)


                .AddObjectiveCollectItem(1, "lt_choker_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
