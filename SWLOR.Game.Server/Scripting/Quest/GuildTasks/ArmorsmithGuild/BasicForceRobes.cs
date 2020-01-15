using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicForceRobes: AbstractQuest
    {
        public BasicForceRobes()
        {
            CreateQuest(104, "Armorsmith Guild Task: 1x Basic Force Robes", "arm_tsk_104")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 0)


                .AddObjectiveCollectItem(1, "force_robe_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
