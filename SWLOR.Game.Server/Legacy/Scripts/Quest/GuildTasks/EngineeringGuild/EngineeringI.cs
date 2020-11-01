using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class EngineeringI: AbstractQuest
    {
        public EngineeringI()
        {
            CreateQuest(389, "Engineering Guild Task: 1x Engineering I", "eng_tsk_389")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_engin1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
