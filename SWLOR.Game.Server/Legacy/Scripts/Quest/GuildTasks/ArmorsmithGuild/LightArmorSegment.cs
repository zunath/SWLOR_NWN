using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorSegment: AbstractQuest
    {
        public LightArmorSegment()
        {
            CreateQuest(120, "Armorsmith Guild Task: 1x Light Armor Segment", "arm_tsk_120")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "l_armor_segment", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 10);
        }
    }
}
