using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class WarocasSpine: AbstractQuest
    {
        public WarocasSpine()
        {
            CreateQuest(584, "Hunter's Guild Task: 6x Warocas Spine", "hun_tsk_584")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "waro_feathers", 6, false)

                .AddRewardGold(68)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 15);
        }
    }
}
