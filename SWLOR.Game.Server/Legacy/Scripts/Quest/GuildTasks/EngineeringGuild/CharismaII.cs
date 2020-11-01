using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CharismaII: AbstractQuest
    {
        public CharismaII()
        {
            CreateQuest(418, "Engineering Guild Task: 1x Charisma II", "eng_tsk_418")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cha2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
