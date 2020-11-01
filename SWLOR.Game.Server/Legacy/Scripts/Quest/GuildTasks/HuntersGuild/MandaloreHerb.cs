using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class MandaloreHerb: AbstractQuest
    {
        public MandaloreHerb()
        {
            CreateQuest(591, "Hunter's Guild Task: 6x Mandalore Herb", "hun_tsk_591")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "herb_m", 6, false)

                .AddRewardGold(76)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 19);
        }
    }
}
