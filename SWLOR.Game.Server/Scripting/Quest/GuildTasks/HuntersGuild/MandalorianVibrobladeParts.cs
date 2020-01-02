using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianVibrobladeParts: AbstractQuest
    {
        public MandalorianVibrobladeParts()
        {
            CreateQuest(598, "Hunter's Guild Task: 6x Mandalorian Vibroblade Parts", "hun_tsk_598")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "m_vibro_parts", 6, false)

                .AddRewardGold(83)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 25);
        }
    }
}
