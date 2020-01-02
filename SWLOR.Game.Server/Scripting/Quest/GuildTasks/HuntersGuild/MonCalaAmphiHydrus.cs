using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MonCalaAmphiHydrus: AbstractQuest
    {
        public MonCalaAmphiHydrus()
        {
            CreateQuest(606, "Hunter's Guild Task: 10x Mon Cala Amphi-Hydrus", "hun_tsk_606")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.MonCala_AmphiHydrus, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(212)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 52);
        }
    }
}
