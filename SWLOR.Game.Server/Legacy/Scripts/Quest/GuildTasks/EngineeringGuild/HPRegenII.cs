using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class HPRegenII: AbstractQuest
    {
        public HPRegenII()
        {
            CreateQuest(475, "Engineering Guild Task: 1x HP Regen II", "eng_tsk_475")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_hpregen2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
