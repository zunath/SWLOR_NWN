using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class KathHoundTooth: AbstractQuest
    {
        public KathHoundTooth()
        {
            CreateQuest(581, "Hunter's Guild Task: 6x Kath Hound Tooth", "hun_tsk_581")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "k_hound_tooth", 6, false)

                .AddRewardGold(67)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 14);
        }
    }
}
