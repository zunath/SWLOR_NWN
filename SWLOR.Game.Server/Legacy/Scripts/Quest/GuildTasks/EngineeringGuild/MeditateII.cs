using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class MeditateII: AbstractQuest
    {
        public MeditateII()
        {
            CreateQuest(437, "Engineering Guild Task: 1x Meditate II", "eng_tsk_437")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_med2", 1, true)

                .AddRewardGold(230)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 50);
        }
    }
}
