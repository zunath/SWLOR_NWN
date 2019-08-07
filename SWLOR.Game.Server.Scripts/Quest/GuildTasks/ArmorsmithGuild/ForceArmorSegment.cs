using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceArmorSegment: AbstractQuest
    {
        public ForceArmorSegment()
        {
            CreateQuest(116, "Armorsmith Guild Task: 1x Force Armor Segment", "arm_tsk_116")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "f_armor_segment", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 15);
        }
    }
}
