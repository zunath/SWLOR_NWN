using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class PowerGloveII: AbstractQuest
    {
        public PowerGloveII()
        {
            CreateQuest(165, "Armorsmith Guild Task: 1x Power Glove II", "arm_tsk_165")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 2)


                .AddObjectiveCollectItem(1, "powerglove_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
