using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PrismaticHeavyBelt: AbstractQuest
    {
        public PrismaticHeavyBelt()
        {
            CreateQuest(217, "Armorsmith Guild Task: 1x Prismatic Heavy Belt", "arm_tsk_217")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "prism_belt_h", 1, true)

                .AddRewardGold(395)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 82);
        }
    }
}
