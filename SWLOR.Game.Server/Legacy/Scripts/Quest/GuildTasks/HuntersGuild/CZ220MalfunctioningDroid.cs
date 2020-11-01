using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class CZ220MalfunctioningDroid: AbstractQuest
    {
        public CZ220MalfunctioningDroid()
        {
            CreateQuest(568, "Hunter's Guild Task: 10x CZ-220 Malfunctioning Droid", "hun_tsk_568")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_MalfunctioningDroids, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(37)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 7);
        }
    }
}
