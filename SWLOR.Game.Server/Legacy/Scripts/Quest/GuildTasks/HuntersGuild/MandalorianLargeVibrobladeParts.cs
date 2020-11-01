using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianLargeVibrobladeParts: AbstractQuest
    {
        public MandalorianLargeVibrobladeParts()
        {
            CreateQuest(595, "Hunter's Guild Task: 6x Mandalorian Large Vibroblade Parts", "hun_tsk_595")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "m_lvibro_parts", 6, false)

                .AddRewardGold(83)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 25);
        }
    }
}
