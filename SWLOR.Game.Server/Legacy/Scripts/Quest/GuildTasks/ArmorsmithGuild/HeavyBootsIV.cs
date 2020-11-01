using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBootsIV: AbstractQuest
    {
        public HeavyBootsIV()
        {
            CreateQuest(202, "Armorsmith Guild Task: 1x Heavy Boots IV", "arm_tsk_202")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_boots_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}
