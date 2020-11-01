using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBootsII: AbstractQuest
    {
        public HeavyBootsII()
        {
            CreateQuest(154, "Armorsmith Guild Task: 1x Heavy Boots II", "arm_tsk_154")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_boots_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
