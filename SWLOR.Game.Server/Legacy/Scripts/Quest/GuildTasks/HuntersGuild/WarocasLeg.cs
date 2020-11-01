using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class WarocasLeg: AbstractQuest
    {
        public WarocasLeg()
        {
            CreateQuest(582, "Hunter's Guild Task: 6x Warocas Leg", "hun_tsk_582")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "waro_leg", 6, false)

                .AddRewardGold(67)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 14);
        }
    }
}
