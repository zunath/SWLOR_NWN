using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorSegment: AbstractQuest
    {
        public LightArmorSegment()
        {
            CreateQuest(120, "Armorsmith Guild Task: 1x Light Armor Segment", "arm_tsk_120")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 0)


                .AddObjectiveCollectItem(1, "l_armor_segment", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 10);
        }
    }
}
