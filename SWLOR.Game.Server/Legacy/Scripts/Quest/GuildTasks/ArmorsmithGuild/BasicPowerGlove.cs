using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicPowerGlove: AbstractQuest
    {
        public BasicPowerGlove()
        {
            CreateQuest(111, "Armorsmith Guild Task: 1x Basic Power Glove", "arm_tsk_111")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "powerglove_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
