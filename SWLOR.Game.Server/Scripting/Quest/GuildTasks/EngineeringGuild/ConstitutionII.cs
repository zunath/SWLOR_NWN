using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ConstitutionII: AbstractQuest
    {
        public ConstitutionII()
        {
            CreateQuest(419, "Engineering Guild Task: 1x Constitution II", "eng_tsk_419")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_con2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
