using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class CZ220MalfunctioningDroid: AbstractQuest
    {
        public CZ220MalfunctioningDroid()
        {
            CreateQuest(568, "Hunter's Guild Task: 10x CZ-220 Malfunctioning Droid", "hun_tsk_568")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveKillTarget(1, NPCGroup.CZ220MalfunctioningDroids, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(37)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 7);
        }
    }
}
