using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
