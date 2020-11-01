using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class StrengthII: AbstractQuest
    {
        public StrengthII()
        {
            CreateQuest(446, "Engineering Guild Task: 1x Strength II", "eng_tsk_446")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_str2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
