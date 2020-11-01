using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class StrengthI: AbstractQuest
    {
        public StrengthI()
        {
            CreateQuest(408, "Engineering Guild Task: 1x Strength I", "eng_tsk_408")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_str1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
