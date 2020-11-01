using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FirstAidII: AbstractQuest
    {
        public FirstAidII()
        {
            CreateQuest(469, "Engineering Guild Task: 1x First Aid II", "eng_tsk_469")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_faid2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
