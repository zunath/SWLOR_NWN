using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBootsI: AbstractQuest
    {
        public HeavyBootsI()
        {
            CreateQuest(131, "Armorsmith Guild Task: 1x Heavy Boots I", "arm_tsk_131")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_boots_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
