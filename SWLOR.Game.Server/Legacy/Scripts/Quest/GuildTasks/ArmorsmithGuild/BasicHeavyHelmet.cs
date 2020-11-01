using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicHeavyHelmet: AbstractQuest
    {
        public BasicHeavyHelmet()
        {
            CreateQuest(106, "Armorsmith Guild Task: 1x Basic Heavy Helmet", "arm_tsk_106")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_hb", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}
