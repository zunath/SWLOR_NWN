using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PowerGloveII: AbstractQuest
    {
        public PowerGloveII()
        {
            CreateQuest(165, "Armorsmith Guild Task: 1x Power Glove II", "arm_tsk_165")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "powerglove_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
