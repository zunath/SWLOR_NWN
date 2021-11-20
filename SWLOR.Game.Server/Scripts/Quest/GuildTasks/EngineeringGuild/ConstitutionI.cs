using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ConstitutionI: AbstractQuest
    {
        public ConstitutionI()
        {
            CreateQuest(380, "Engineering Guild Task: 1x Constitution I", "eng_tsk_380")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_con1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
