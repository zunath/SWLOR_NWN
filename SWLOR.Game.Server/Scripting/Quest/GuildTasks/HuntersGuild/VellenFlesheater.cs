using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class VellenFlesheater: AbstractQuest
    {
        public VellenFlesheater()
        {
            CreateQuest(610, "Hunter's Guild Task: 10x Vellen Flesheater", "hun_tsk_610")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroup.VellenFlesheater, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(184)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 44);
        }
    }
}
