using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class MeditateI: AbstractQuest
    {
        public MeditateI()
        {
            CreateQuest(436, "Engineering Guild Task: 1x Meditate I", "eng_tsk_436")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_med1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
