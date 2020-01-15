using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class ViscaraKathHound: AbstractQuest
    {
        public ViscaraKathHound()
        {
            CreateQuest(570, "Hunter's Guild Task: 10x Viscara Kath Hound", "hun_tsk_570")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveKillTarget(1, NPCGroup.ViscaraWildlandKathHounds, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
