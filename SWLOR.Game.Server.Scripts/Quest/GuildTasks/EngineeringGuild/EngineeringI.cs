using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
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
