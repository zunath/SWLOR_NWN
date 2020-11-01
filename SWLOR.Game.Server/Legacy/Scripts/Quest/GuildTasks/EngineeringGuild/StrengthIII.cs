using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class StrengthIII: AbstractQuest
    {
        public StrengthIII()
        {
            CreateQuest(555, "Engineering Guild Task: 1x Strength III", "eng_tsk_555")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_str3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
