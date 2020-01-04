using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianRanger: AbstractQuest
    {
        public MandalorianRanger()
        {
            CreateQuest(586, "Hunter's Guild Task: 10x Mandalorian Ranger", "hun_tsk_586")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroup.MandalorianRangers, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(76)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 19);
        }
    }
}
