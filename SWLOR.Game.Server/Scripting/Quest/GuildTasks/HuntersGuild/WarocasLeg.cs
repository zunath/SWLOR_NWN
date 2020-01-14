using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class WarocasLeg: AbstractQuest
    {
        public WarocasLeg()
        {
            CreateQuest(582, "Hunter's Guild Task: 6x Warocas Leg", "hun_tsk_582")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveCollectItem(1, "waro_leg", 6, false)

                .AddRewardGold(67)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 14);
        }
    }
}
