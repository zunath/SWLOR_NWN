using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
