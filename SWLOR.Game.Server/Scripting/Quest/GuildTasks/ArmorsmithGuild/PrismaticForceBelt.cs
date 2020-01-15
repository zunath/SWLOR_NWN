using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class PrismaticForceBelt: AbstractQuest
    {
        public PrismaticForceBelt()
        {
            CreateQuest(216, "Armorsmith Guild Task: 1x Prismatic Force Belt", "arm_tsk_216")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 4)


                .AddObjectiveCollectItem(1, "prism_belt_f", 1, true)

                .AddRewardGold(395)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 82);
        }
    }
}
