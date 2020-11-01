using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CharismaI: AbstractQuest
    {
        public CharismaI()
        {
            CreateQuest(379, "Engineering Guild Task: 1x Charisma I", "eng_tsk_379")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cha1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
