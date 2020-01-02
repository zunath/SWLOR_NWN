using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class WildwoodsKinrath: AbstractQuest
    {
        public WildwoodsKinrath()
        {
            CreateQuest(574, "Hunter's Guild Task: 10x Wildwoods Kinrath", "hun_tsk_574")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildwoodsKinraths, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 13);
        }
    }
}
