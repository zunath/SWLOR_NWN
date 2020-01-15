using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class ValleyRaivor: AbstractQuest
    {
        public ValleyRaivor()
        {
            CreateQuest(589, "Hunter's Guild Task: 10x Valley Raivor", "hun_tsk_589")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 1)


                .AddObjectiveKillTarget(1, NPCGroup.ViscaraDeepMountainRaivors, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(84)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 27);
        }
    }
}
