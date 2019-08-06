using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FirstAidI: AbstractQuest
    {
        public FirstAidI()
        {
            CreateQuest(391, "Engineering Guild Task: 1x First Aid I", "eng_tsk_391")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_faid1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
