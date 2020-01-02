using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MynockWing: AbstractQuest
    {
        public MynockWing()
        {
            CreateQuest(575, "Hunter's Guild Task: 6x Mynock Wing", "hun_tsk_575")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "mynock_wing", 6, false)

                .AddRewardGold(23)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 7);
        }
    }
}
