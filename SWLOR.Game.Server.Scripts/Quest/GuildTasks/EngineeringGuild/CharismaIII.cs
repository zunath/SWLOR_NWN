using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CharismaIII: AbstractQuest
    {
        public CharismaIII()
        {
            CreateQuest(510, "Engineering Guild Task: 1x Charisma III", "eng_tsk_510")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cha3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
