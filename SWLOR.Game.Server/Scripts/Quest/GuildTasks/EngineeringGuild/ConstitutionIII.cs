using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ConstitutionIII: AbstractQuest
    {
        public ConstitutionIII()
        {
            CreateQuest(512, "Engineering Guild Task: 1x Constitution III", "eng_tsk_512")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_con3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
