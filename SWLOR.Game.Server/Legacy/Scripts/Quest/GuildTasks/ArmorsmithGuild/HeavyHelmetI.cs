using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyHelmetI: AbstractQuest
    {
        public HeavyHelmetI()
        {
            CreateQuest(133, "Armorsmith Guild Task: 1x Heavy Helmet I", "arm_tsk_133")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "helmet_h1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
