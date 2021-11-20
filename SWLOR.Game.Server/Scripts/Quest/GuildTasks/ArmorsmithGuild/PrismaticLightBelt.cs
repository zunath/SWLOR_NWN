using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PrismaticLightBelt: AbstractQuest
    {
        public PrismaticLightBelt()
        {
            CreateQuest(218, "Armorsmith Guild Task: 1x Prismatic Light Belt", "arm_tsk_218")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "prism_belt_l", 1, true)

                .AddRewardGold(395)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 82);
        }
    }
}
