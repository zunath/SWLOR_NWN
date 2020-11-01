using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBootsIII: AbstractQuest
    {
        public HeavyBootsIII()
        {
            CreateQuest(180, "Armorsmith Guild Task: 1x Heavy Boots III", "arm_tsk_180")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_boots_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}
