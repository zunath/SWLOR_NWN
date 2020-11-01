using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicForceHelmet: AbstractQuest
    {
        public BasicForceHelmet()
        {
            CreateQuest(103, "Armorsmith Guild Task: 1x Basic Force Helmet", "arm_tsk_103")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_fb", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
