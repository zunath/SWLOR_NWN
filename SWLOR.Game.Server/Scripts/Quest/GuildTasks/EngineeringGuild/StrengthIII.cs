using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
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
