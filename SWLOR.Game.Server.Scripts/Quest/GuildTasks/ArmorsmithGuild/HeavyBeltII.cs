using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyBeltII: AbstractQuest
    {
        public HeavyBeltII()
        {
            CreateQuest(153, "Armorsmith Guild Task: 1x Heavy Belt II", "arm_tsk_153")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "heavy_belt_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}
