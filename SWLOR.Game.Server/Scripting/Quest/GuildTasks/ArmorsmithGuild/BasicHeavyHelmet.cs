using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicHeavyHelmet: AbstractQuest
    {
        public BasicHeavyHelmet()
        {
            CreateQuest(106, "Armorsmith Guild Task: 1x Basic Heavy Helmet", "arm_tsk_106")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 0)


                .AddObjectiveCollectItem(1, "helmet_hb", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
