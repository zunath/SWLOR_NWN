using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBeltI: AbstractQuest
    {
        public HeavyBeltI()
        {
            CreateQuest(130, "Armorsmith Guild Task: 1x Heavy Belt I", "arm_tsk_130")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_belt_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}
