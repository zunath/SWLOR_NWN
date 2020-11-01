using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HullPlating: AbstractQuest
    {
        public HullPlating()
        {
            CreateQuest(205, "Armorsmith Guild Task: 1x Hull Plating", "arm_tsk_205")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "hull_plating", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 90);
        }
    }
}
