using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicHeavyBoots: AbstractQuest
    {
        public BasicHeavyBoots()
        {
            CreateQuest(105, "Armorsmith Guild Task: 1x Basic Heavy Boots", "arm_tsk_105")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 0)


                .AddObjectiveCollectItem(1, "heavy_boots_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
